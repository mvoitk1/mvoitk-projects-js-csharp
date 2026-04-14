if this template isnt plain jet make it plain so i can create a new project on it

---

dont write any code yet. I have a new project at hand here is the assignment and the project proposal that i have done: Assignment 3: Personal Project - Phase 1 
Based on your personal project proposal!

Implement your project in aspnet.core, without doing the full architectural dance. No hard requirement for CLEAN/ONION approach (do it if you want do).

What must be there:

domain design
rest api controllers, versioning, public dtos
swagger
auth (jwt)
client UX (mvc)
admin UX (mvc)
translations in UI
translations in DB
idor (user can only access their own data)
ci/cd deploy
Repositories, UOW, servives, BLL - not mandatory.
Do not just scaffold all the controllers - do them as UX requires.

Using your own backend rest api and JWT/refresh_token authentication.
Deploy the client to your VPS (separate docker/domain) - if it is web based.

lets write a documentation/implementation plan and a CLAUDE.md for this project. ask me questions if you have any, propose changes if needed, the proposal is in the pdf.

---


brändi e-kaubanduse platvorm.pdf
README.md
dont write any code yet. I have a new project at hand here is the assignment and the project proposal that i have done: Assignment 3: Personal Project - Phase 1 
Based on your personal project proposal!

Implement your project in aspnet.core, without doing the full architectural dance. No hard requirement for CLEAN/ONION approach (do it if you want do).

What must be there:

domain design
rest api controllers, versioning, public dtos
swagger
auth (jwt)
client UX (mvc)
admin UX (mvc)
translations in UI
translations in DB
idor (user can only access their own data)
ci/cd deploy
Repositories, UOW, servives, BLL - not mandatory.
Do not just scaffold all the controllers - do them as UX requires.

Using your own backend rest api and JWT/refresh_token authentication.
Deploy the client to your VPS (separate docker/domain) - if it is web based.

lets write a documentation/implementation plan and a CLAUDE.md for this project. ask me questions if you have any, propose changes if needed, the proposal is in the pdf.
Let me first look at the existing project structure and README.

Good, I have a solid picture of the template. Before writing the plan, I have a few questions:

Questions before writing the plan:

1. Architecture scope
The assignment says "no hard requirement for CLEAN/ONION." The template already has App.BLL, App.DAL.EF, App.DTO, App.Domain projects. Do you want to use all of them (i.e., keep Repositories/Services/BLL layers), or slim it down to just Domain + DAL + WebApp (direct DbContext calls in controllers)?

2. ERD vs. assignment scope
Your ERD is quite large (Product, ProductVariant, Color, Size, Cart, CartItem, Order, OrderItem, Address, Collection, Category, ProductImage, Review, AppUser, AppRole). The assignment says do them as UX requires — not all controllers. Do you want to implement the full ERD, or a scoped subset for this phase? My suggestion would be to scope to the critical path: Product/Variant/Category/Collection + Cart + Order + User/Auth + Admin CRUD — and skip Review and Address for now.

3. Payment
Your proposal shows payment options (Swedbank, SEB, LHV, Luminor, Visa). Is actual payment integration in scope for Assignment 3, or is "place order" with a dummy payment step sufficient?

4. Translations
The assignment requires both UI translations and DB translations. Your ERD doesn't have a LangStr/translation table — are you OK with me proposing a LangStr pattern (like the template uses) for translatable fields (e.g., product name, description, category name)? Which fields need DB translation?

5. Deployment
Do you have a VPS already set up, and does it have a domain/subdomain ready for this project? What's the stack — Nginx + Docker Compose?

6. Client vs. Admin UX separation
The assignment requires separate Client UX and Admin UX (both MVC). Should the admin be in a separate /Admin area (like the template suggests with Areas/Admin/Controllers) and the customer-facing shop be in the default area?

7. Separate client deployment
The assignment says "deploy the client to your VPS (separate docker/domain)." Does this mean the MVC client app should be a separate project/container from the API backend, calling the REST API with JWT? Or is the MVC app in the same WebApp project (which is more typical for .NET MVC + API in one)?

Proposals I'd make regardless of your answers:

Skip Address entity for now (hardcode shipping address as text fields on Order)
Skip Review for Phase 1
ProductImage — keep as URLs (no file upload, just string field) for simplicity
The ERD uses int PKs but the template uses Guid — I'd switch everything to Guid to match the template
1. no lets do CLEAN/ONION and lets use the projects given so the solution names and stuff would be nice(can add more projects).
2. ok, lets cut the initial scope a bit, but the cut stuff needs to be documented so the next person or ai can see that those thing are yet to be implemented.
3. its in scope, but lets skip it for now, document it that it needs to be done.
4. Yes, go with the LangStr pattern. The fields that need it are Product (Name, Description, Material), Category (Name), Collection (Name, Description), Color (Name), Size (DisplayName), and ProductImage (AltText). Everything else — SKUs, prices, addresses, user data, order fields — stays as plain strings since they're either numeric, user-entered, or enum-based.
5. yes, the vps is 192.168.181.91:82, nginx + docker compose.
6. yes.
7. Just go with the standard one solution approach: API and MVC in the same project or same solution, sharing the same backend, no need to split into separate containers. Keep it simple — one Docker container, one domain, MVC calls the DB directly (or through lightweight service classes), and the REST API sits alongside it for the JS client you'll build in the other course. The gitlab-ci.yml is in the parents parent folder.

yes fk-s in guid, everything that is skipped in this phase needs to be documented,
create documentation.md, implementation_plan.md and claude.md for the solution at hand, if you want to create anything else let me know.

---

 **5h limit**

---

