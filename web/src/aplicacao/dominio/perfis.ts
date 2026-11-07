// Texto exibido de cada perfil. O valor que vai e volta da API não muda ("Usuario", sem acento,
// como no back-end); só o rótulo na tela recebe o acento. Os demais perfis já se escrevem igual.
const ROTULOS: Record<string, string> = { Usuario: "Usuário" };

export function rotuloPerfil(perfil: string | null | undefined): string {
  const valor = String(perfil ?? "").trim();
  return ROTULOS[valor] ?? valor;
}
