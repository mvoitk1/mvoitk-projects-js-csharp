# ERD

This ERD reflects what is implemented in the codebase and initial migration as of April 16, 2026.

## Current schema

```mermaid
erDiagram
    APP_USER {
        uuid Id PK
        string FirstName
        string LastName
        string Email
        string Phone
    }

    APP_ROLE {
        uuid Id PK
        string Name
    }

    APP_REFRESH_TOKEN {
        uuid Id PK
        string RefreshToken
        datetime Expiration
        string PreviousRefreshToken
        datetime PreviousExpiration
        uuid UserId FK
    }

    CATEGORY {
        uuid Id PK
        jsonb Name
        uuid ParentCategoryId FK
    }

    COLLECTION {
        uuid Id PK
        jsonb Name
        jsonb Description
        datetime LaunchDate
        bool IsActive
    }

    COLOR {
        uuid Id PK
        jsonb Name
        string HexCode
    }

    SIZE {
        uuid Id PK
        string SizeCode
        jsonb DisplayName
    }

    PRODUCT {
        uuid Id PK
        jsonb Name
        jsonb Description
        jsonb Material
        int Gender
        bool IsActive
        datetime CreatedAt
        uuid CollectionId FK
    }

    PRODUCT_CATEGORY {
        uuid ProductId PK, FK
        uuid CategoryId PK, FK
        datetime From
        datetime Until
    }

    PRODUCT_IMAGE {
        uuid Id PK
        string Url
        jsonb AltText
        int SortOrder
        uuid ProductId FK
    }

    PRODUCT_VARIANT {
        uuid Id PK
        string Sku
        decimal Price
        decimal UnitPrice
        int StockQty
        bool IsActive
        uuid ColorId FK
        uuid SizeId FK
        uuid ProductId FK
    }

    CART {
        uuid Id PK
        int Status
        datetime CreatedAt
        datetime UpdatedAt
        uuid AppUserId FK
    }

    CART_ITEM {
        uuid Id PK
        int Quantity
        decimal UnitPrice
        uuid CartId FK
        uuid ProductVariantId FK
    }

    ORDER {
        uuid Id PK
        string OrderNumber
        int Status
        decimal TotalAmount
        datetime CreatedAt
        string ShippingFirstName
        string ShippingLastName
        string ShippingEmail
        string ShippingPhone
        string ShippingCountry
        string ShippingCity
        string ShippingStreet
        string ShippingPostalCode
        uuid AppUserId FK
    }

    ORDER_ITEM {
        uuid Id PK
        int Quantity
        decimal UnitPrice
        decimal LineTotal
        uuid OrderId FK
        uuid ProductVariantId FK
    }

    APP_USER ||--o{ APP_REFRESH_TOKEN : has
    APP_USER }o--o{ APP_ROLE : assigned
    APP_USER ||--o{ CART : owns
    APP_USER ||--o{ ORDER : places

    CATEGORY ||--o{ CATEGORY : parent_of
    COLLECTION ||--o{ PRODUCT : groups
    PRODUCT ||--o{ PRODUCT_IMAGE : has
    PRODUCT ||--o{ PRODUCT_VARIANT : has
    PRODUCT ||--o{ PRODUCT_CATEGORY : linked_by
    CATEGORY ||--o{ PRODUCT_CATEGORY : linked_by

    COLOR ||--o{ PRODUCT_VARIANT : colors
    SIZE ||--o{ PRODUCT_VARIANT : sizes

    CART ||--o{ CART_ITEM : contains
    PRODUCT_VARIANT ||--o{ CART_ITEM : selected_as

    ORDER ||--o{ ORDER_ITEM : contains
    PRODUCT_VARIANT ||--o{ ORDER_ITEM : snapshot_of
```

## Notes

- `LangStr` fields are stored as `jsonb` for multilingual content.
- `ProductCategory` is the only composite primary key table.
- Category is self-referencing through `ParentCategoryId`.
- Shipping address is currently stored directly on `Order` as flat fields.
- Global delete behavior is configured as `Restrict`.

## Seeded data currently present

- Roles: `Admin`, `Customer`
- Seeded admin user: `admin@shop.ee`
- Seeded catalogue basics:
  - 3 colors
  - 5 sizes
  - 3 categories
  - 1 collection
  - 1 product with image, variants, and category link

## Planned but not implemented in the schema yet

These are mentioned in the docs, but are not part of the current EF model or migration:

- `Review`
- `Address`
- `Wishlist` / `WishlistItem`
