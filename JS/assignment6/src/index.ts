import "dotenv/config"; // side-effect import — loads .env into process.env
import app from "./app.js";

const PORT = Number(process.env.PORT) || 3000;

app.listen(PORT, "0.0.0.0", () => {
  console.log(`Server running on http://0.0.0.0:${PORT}`);
});
