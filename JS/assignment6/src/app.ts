import express from "express";
import cors from "cors";
import { resolve } from "node:path";
import accountRouter from "./routes/account.js";
import tasksRouter from "./routes/tasks.js";
import categoriesRouter from "./routes/categories.js";
import prioritiesRouter from "./routes/priorities.js";
import { adminController } from "./controllers/adminController.js";

const app = express();

app.set("view engine", "ejs");
app.set("views", resolve(process.env.TEMPLATES_DIR ?? "templates"));

app.use(cors());
app.use(express.json());

app.use((req, res, next) => {
  const start = Date.now();
  res.on("finish", () => {
    const ms = Date.now() - start;
    console.log(`${req.method} ${req.url} ${res.statusCode} ${ms}ms`);
  });
  next();
});

for (const prefix of ["/api/v1", "/api/v1.0"]) {
  app.use(`${prefix}/Account`, accountRouter);
  app.use(`${prefix}/TodoTasks`, tasksRouter);
  app.use(`${prefix}/TodoCategories`, categoriesRouter);
  app.use(`${prefix}/TodoPriorities`, prioritiesRouter);
}

app.get("/overview", adminController.overview);

app.get("/", (_req, res) => {
  res.json({ name: "assignment6 api", ok: true, overview: "/overview" });
});

export default app;
