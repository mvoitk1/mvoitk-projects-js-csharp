import { Router } from "express";
import { requireAuth } from "../auth.js";
import { categoriesController } from "../controllers/categoriesController.js";

const router = Router();
router.use(requireAuth);

router.get("/", categoriesController.list);
router.get("/:id", categoriesController.get);
router.post("/", categoriesController.create);
router.put("/:id", categoriesController.update);
router.delete("/:id", categoriesController.remove);

export default router;
