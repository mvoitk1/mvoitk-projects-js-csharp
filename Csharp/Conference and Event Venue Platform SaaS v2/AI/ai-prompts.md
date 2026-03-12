/opsx-propose.md go over the @/README.md  and create me a config.yaml

in the root create a AGENTS.md aswell

in the agents.md and config.yaml add that the code should be: clean code, kiss, dry, yagni, onion

Everything in the project is just a base to build off of. Existing scaffolded roles can be changed according to the implementation plan. The project target framework is .NET 10 / `net10.0`. Future AI agents should treat the active OpenSpec change and implementation artifacts as the source of product truth over starter-template assumptions.

The intended role model is `User`, `CompanyEmployee`, `CompanyManager`, and `Admin`. Also note that some current web assets are likely unrelated leftovers, especially `WebApp/wwwroot/js/screens/*`, and should not be preserved unless intentionally reused by the implementation.
