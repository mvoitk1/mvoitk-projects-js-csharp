# mvoitk-projects

**Madis Voitk** | 250627IADB | mvoitk@taltech.ee | mvoitk

Monorepo containing school projects built across JavaScript/TypeScript and C#/.NET. All deployed projects are accessible at **https://mvoitk.proxy.itcollege.ee** — see individual links below.

---

## JavaScript / TypeScript Projects (`JS/`)

### [assignment1](JS/assignment1) — Vanilla JS Task Manager
Task manager with dashboard, task list, calendar view, command palette, and dark theme. Data persists in localStorage.

- **Stack:** HTML5, CSS3, ES6+, esbuild
- **Live:** https://mvoitk-js1.proxy.itcollege.ee

### [assignment2](JS/assignment2) — Task Forge (TypeScript)
TypeScript rewrite of the task manager with dashboard, list, and calendar views. Bundled with Vite, tested with Vitest, served via Nginx in Docker.

- **Stack:** TypeScript, Vite, Vitest, Docker + Nginx
- **Live:** https://mvoitk-js2.proxy.itcollege.ee

### [assignment4](JS/assignment4) — Vue ToDo App (v1)
Vue 3 SPA consuming the TalTech REST API for ToDo management. Includes JWT + refresh token auth flow, route guards, and Pinia state management.

- **Stack:** Vue 3, TypeScript, Vite, Pinia, Vue Router, Axios

### [assignment4.1](JS/assignment4.1) — Vue ToDo App (v2)
Enhanced version of the Vue ToDo app with Composition API throughout, stricter TypeScript, ESLint + Prettier, and a refined auth/token refresh flow.

- **Stack:** Vue 3 (Composition API), TypeScript 6, Vite 8, Pinia 3, Vue Router 5, Axios
- **Live:** https://mvoitk-vue2.proxy.itcollege.ee

### [vue-listitems](JS/vue-listitems) — Vue List Items
Pre-built Vue 3 app containerised and deployed via Nginx.

- **Stack:** Vue 3, Vite, Docker + Nginx

### [vue_front_to_PP](JS/vue_front_to_PP) — E-commerce Frontend
Vue 3 frontend for the C# e-commerce platform (Assignment 3). Features product catalog, shopping cart, order management, admin dashboard, and EN/ET localisation.

- **Stack:** Vue 3 (Composition API), TypeScript, Vite, Pinia, Vue Router, oxlint, ESLint + Prettier

---

## C# / .NET Projects (`Csharp/`)

### [Assignment_3](Csharp/Assignment_3) — E-commerce Platform
Full-stack e-commerce platform with an ASP.NET Core MVC Razor storefront and a REST API backend. Includes JWT authentication, xUnit tests, and Docker deployment. The [vue_front_to_PP](JS/vue_front_to_PP) project serves as its Vue frontend.

- **Stack:** .NET 10, ASP.NET Core MVC, Entity Framework Core, PostgreSQL, JWT auth, xUnit, Docker
- **Live (API + Razor store):** https://mvoitk-cs3.proxy.itcollege.ee

### [Conference and Event Venue Platform SaaS](Csharp/Conference%20and%20Event%20Venue%20Platform%20SaaS) — Venue Booking SaaS (v1)
SaaS platform for managing conference rooms, equipment, and catering for corporate events. Supports recurring bookings, invoicing, and role-based access (Employee / Manager / Admin).

- **Stack:** ASP.NET Core MVC, EF Core, PostgreSQL, N-tier architecture (Domain / DAL / BLL / UI)

### [Conference and Event Venue Platform SaaS v2](Csharp/Conference%20and%20Event%20Venue%20Platform%20SaaS%20v2) — Venue Booking SaaS (v2)
Evolved architecture of the venue platform with Docker Compose for local and production deployment.

- **Stack:** .NET, ASP.NET Core, EF Core, PostgreSQL, Docker Compose
