import { usarAcaoUnica } from "./usarAcaoUnica";

// Etapa 6.4-B corretiva: Salvar e Consolidar ignoram o segundo acionamento enquanto
// a requisição está em andamento.
describe("usarAcaoUnica", () => {
  it("ignora o segundo acionamento enquanto o primeiro está em andamento", async () => {
    const { executar, emAndamento } = usarAcaoUnica();
    let chamadas = 0;
    let liberar!: () => void;
    const acao = () =>
      new Promise<void>((resolve) => {
        chamadas++;
        liberar = resolve;
      });

    const primeira = executar(acao);
    const segunda = executar(acao);

    expect(emAndamento.value).toBe(true);
    expect(chamadas).toBe(1);
    await expect(segunda).resolves.toBeUndefined();

    liberar();
    await primeira;
    expect(emAndamento.value).toBe(false);
  });

  it("libera de novo depois de terminar, inclusive com erro", async () => {
    const { executar, emAndamento } = usarAcaoUnica();
    await expect(executar(async () => { throw new Error("falhou"); })).rejects.toThrow("falhou");
    expect(emAndamento.value).toBe(false);
    await expect(executar(async () => 42)).resolves.toBe(42);
  });
});
