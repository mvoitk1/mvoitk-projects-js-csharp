import { Router } from "express";
import { accountController } from "../controllers/accountController.js";

const router = Router();

router.post("/Login", accountController.login);
router.post("/Register", accountController.register);
router.post("/RefreshToken", accountController.refreshToken);

export default router;
