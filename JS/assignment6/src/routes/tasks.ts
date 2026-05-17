import { Router } from "express";
import { requireAuth } from "../auth.js";
import { tasksController } from "../controllers/tasksController.js";

const router = Router();
router.use(requireAuth);

router.get("/", tasksController.list);
router.get("/:id", tasksController.get);
router.post("/", tasksController.create);
router.put("/:id", tasksController.update);
router.delete("/:id", tasksController.remove);

export default router;
