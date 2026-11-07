import { describe, expect, it } from "vitest";

import { rotuloPerfil } from "./perfis";

describe("rotuloPerfil", () => {
  it("exibe Usuario com acento e mantém os demais perfis", () => {
    expect(rotuloPerfil("Usuario")).toBe("Usuário");
    for (const perfil of ["Admin", "Pastor", "Superintendente", "Professor"]) {
      expect(rotuloPerfil(perfil)).toBe(perfil);
    }
  });

  it("vazio ou ausente fica vazio", () => {
    expect(rotuloPerfil("")).toBe("");
    expect(rotuloPerfil(null)).toBe("");
    expect(rotuloPerfil(undefined)).toBe("");
  });
});
