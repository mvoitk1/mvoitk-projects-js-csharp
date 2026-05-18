using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using App.DTO.v1.Cart;
using App.DTO.v1.Orders;
using Base.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using WebApp.Tests.Helpers;
using Xunit;

namespace WebApp.Tests.Integration.Api;

/// <summary>
/// Cart and checkout flows over HTTP: add / update / remove, the IDOR guard that stops one
/// user touching another's cart line, and the register → cart → place-order happy path.
/// </summary>
[Collection("Database tests")]
public class IntegrationTestCart : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public IntegrationTestCart(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static StringContent Json<T>(T value) =>
        new(JsonSerializer.Serialize(value, JsonHelpers.JsonSerializerOptionsCamelCase),
            Encoding.UTF8, "application/json");

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = CreateClient();
        var jwt = await IdentityHelper.SetupUserAsync(client, "Cart", "Tester", "Cart.Tester.1", email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.JWT);
        return client;
    }

    /// <summary>Resolve the id of a seeded product variant via the public product endpoints.</summary>
    private async Task<Guid> GetFirstVariantIdAsync(HttpClient client)
    {
        var listBody = await (await client.GetAsync("/api/v1/products")).Content.ReadAsStringAsync();
        using var listDoc = JsonDocument.Parse(listBody);
        var productId = listDoc.RootElement[0].GetProperty("id").GetGuid();

        var detailBody = await (await client.GetAsync($"/api/v1/products/{productId}")).Content.ReadAsStringAsync();
        using var detailDoc = JsonDocument.Parse(detailBody);
        return detailDoc.RootElement.GetProperty("variants")[0].GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task AddUpdateRemove_CartItem_HappyPath()
    {
        using var client = await CreateAuthenticatedClientAsync("cart.flow@shop.test");
        var variantId = await GetFirstVariantIdAsync(client);

        // Add
        var addResponse = await client.PostAsync("/api/v1/cart/items",
            Json(new AddToCartDto { ProductVariantId = variantId, Quantity = 1 }));
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        var cart = JsonSerializer.Deserialize<CartDto>(
            await addResponse.Content.ReadAsStringAsync(), JsonHelpers.JsonSerializerOptionsCamelCase);
        var cartItemId = Assert.Single(cart!.Items).Id;

        // Update quantity
        var updateResponse = await client.PutAsync($"/api/v1/cart/items/{cartItemId}",
            Json(new UpdateCartItemDto { Quantity = 3 }));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedCart = JsonSerializer.Deserialize<CartDto>(
            await updateResponse.Content.ReadAsStringAsync(), JsonHelpers.JsonSerializerOptionsCamelCase);
        Assert.Equal(3, Assert.Single(updatedCart!.Items).Quantity);

        // Remove
        var removeResponse = await client.DeleteAsync($"/api/v1/cart/items/{cartItemId}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);
    }

    [Fact]
    public async Task RemoveCartItem_OfAnotherUser_IsRejected()
    {
        using var clientA = await CreateAuthenticatedClientAsync("cart.idor.a@shop.test");
        using var clientB = await CreateAuthenticatedClientAsync("cart.idor.b@shop.test");
        var variantId = await GetFirstVariantIdAsync(clientA);

        // User A puts an item in their cart
        var addResponse = await clientA.PostAsync("/api/v1/cart/items",
            Json(new AddToCartDto { ProductVariantId = variantId, Quantity = 1 }));
        var cart = JsonSerializer.Deserialize<CartDto>(
            await addResponse.Content.ReadAsStringAsync(), JsonHelpers.JsonSerializerOptionsCamelCase);
        var cartItemIdOfA = Assert.Single(cart!.Items).Id;

        // User B tries to delete user A's cart item — must not succeed
        var idorResponse = await clientB.DeleteAsync($"/api/v1/cart/items/{cartItemIdOfA}");
        Assert.NotEqual(HttpStatusCode.NoContent, idorResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, idorResponse.StatusCode);

        // And the item is still in user A's cart
        var stillThere = await clientA.GetAsync("/api/v1/cart");
        var cartAfter = JsonSerializer.Deserialize<CartDto>(
            await stillThere.Content.ReadAsStringAsync(), JsonHelpers.JsonSerializerOptionsCamelCase);
        Assert.Single(cartAfter!.Items);
    }

    [Fact]
    public async Task PlaceOrder_FromCart_Returns201WithOrderBody()
    {
        using var client = await CreateAuthenticatedClientAsync("order.happy@shop.test");
        var variantId = await GetFirstVariantIdAsync(client);

        await client.PostAsync("/api/v1/cart/items",
            Json(new AddToCartDto { ProductVariantId = variantId, Quantity = 2 }));

        var orderResponse = await client.PostAsync("/api/v1/orders", Json(new CreateOrderDto
        {
            ShippingFirstName = "Order",
            ShippingLastName = "Happy",
            ShippingEmail = "order.happy@shop.test",
            ShippingPhone = "+372 5000 0000",
            ShippingCountry = "Estonia",
            ShippingCity = "Tallinn",
            ShippingStreet = "Akadeemia tee 1",
            ShippingPostalCode = "12345"
        }));

        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var order = JsonSerializer.Deserialize<OrderDto>(
            await orderResponse.Content.ReadAsStringAsync(), JsonHelpers.JsonSerializerOptionsCamelCase);
        Assert.NotNull(order);
        Assert.NotEqual(Guid.Empty, order!.Id);
        Assert.Equal("Confirmed", order.Status);
        Assert.Single(order.Items);
    }

    [Fact]
    public async Task PlaceOrder_WithEmptyCart_Returns400()
    {
        using var client = await CreateAuthenticatedClientAsync("order.empty@shop.test");

        var orderResponse = await client.PostAsync("/api/v1/orders", Json(new CreateOrderDto
        {
            ShippingFirstName = "Empty",
            ShippingLastName = "Cart",
            ShippingEmail = "order.empty@shop.test",
            ShippingPhone = "+372 5000 0000",
            ShippingCountry = "Estonia",
            ShippingCity = "Tallinn",
            ShippingStreet = "Akadeemia tee 1",
            ShippingPostalCode = "12345"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, orderResponse.StatusCode);
    }
}
