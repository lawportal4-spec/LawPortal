import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  // Railway assigns a dynamic *.up.railway.app domain we can't know in advance, so the preview
  // server (what actually runs in production — see package.json's "preview" script) needs to
  // accept any Host header rather than a fixed allowlist.
  preview: {
    allowedHosts: true,
  },
});
