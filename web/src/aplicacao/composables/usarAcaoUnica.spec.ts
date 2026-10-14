import { usarAcaoUnica } from "./usarAcaoUnica";

// Etapa 6.4-B corretiva: Salvar e Consolidar ignoram o segundo acionamento enquanto
// a requisição está em andamento — e, desde o ajuste do toque duplo, também por
// 500 ms depois de ela terminar.
describe("usarAcaoUnica", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

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
    // Ainda bloqueado durante a espera; libera depois dela.
    expect(emAndamento.value).toBe(true);
    await vi.advanceTimersByTimeAsync(500);
    expect(emAndamento.value).toBe(false);
  });

  it("toque duplo: o segundo acionamento logo após uma execução rápida é ignorado", async () => {
    const { executar } = usarAcaoUnica();
    let chamadas = 0;
    const rapida = async () => {
      chamadas++;
    };

    await executar(rapida); // termina de imediato
    await vi.advanceTimersByTimeAsync(80); // intervalo medido entre os dois toques
    await executar(rapida);
    expect(chamadas).toBe(1);

    await vi.advanceTimersByTimeAsync(500);
    await executar(rapida);
    expect(chamadas).toBe(2);
  });

  it("libera de novo depois de terminar, inclusive com erro", async () => {
    const { executar, emAndamento } = usarAcaoUnica(0);
    await expect(executar(async () => { throw new Error("falhou"); })).rejects.toThrow("falhou");
    expect(emAndamento.value).toBe(false);
    await expect(executar(async () => 42)).resolves.toBe(42);
  });
});
