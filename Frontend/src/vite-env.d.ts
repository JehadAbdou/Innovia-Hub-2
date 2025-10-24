/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL: string;
  // Lägg till fler environment variabler här om du har
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
declare module "*.css";