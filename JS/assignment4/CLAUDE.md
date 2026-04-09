# Project Overview

This project consists of a Vue 3 frontend SPA that consumes the REST API at https://taltech.akaver.com/.

## Assignment

Build a Vue 3 app against the taltech.akaver.com backend, targeting ToDo entities.
- Implement JWT and refresh token based security
- Use Vue Router and Pinia
- Deploy as a separate Docker container to VPS
- Full project base with proper security that can be reused in personal projects

API Swagger: https://taltech.akaver.com/swagger/index.html

## Architecture

- **Backend**: ASP.NET Core Web API at https://taltech.akaver.com/ (external, not owned)
- **Frontend**: Vue 3 SPA — JWT auth, Pinia state, Vue Router, Axios

## Goals

- Implement JWT + refresh token auth flow (login, register, silent refresh)
- CRUD operations on ToDo entities via REST API
- Pinia stores for auth and todo state
- Vue Router with route guards for protected pages
- Docker containerized deployment to VPS at mvoitk-vue.proxy.itcollege.ee → 192.168.181.91:77