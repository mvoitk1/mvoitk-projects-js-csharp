import { Router } from "express";
import { requireAuth } from "../auth.js";
import { prioritiesController } from "../controllers/prioritiesController.js";

const router = Router();
router.use(requireAuth);

router.get("/", prioritiesController.list);
router.get("/:id", prioritiesController.get);
router.post("/", prioritiesController.create);
router.put("/:id", prioritiesController.update);
router.delete("/:id", prioritiesController.remove);

export default router;
