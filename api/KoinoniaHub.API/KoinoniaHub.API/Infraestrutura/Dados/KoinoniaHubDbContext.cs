using KoinoniaHub.API.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Infraestrutura.Dados
{
    public class KoinoniaHubDbContext : DbContext
    {
        public KoinoniaHubDbContext(DbContextOptions<KoinoniaHubDbContext> options) : base(options) { }

        public DbSet<Igreja> Igrejas => Set<Igreja>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Pessoa> Pessoas => Set<Pessoa>();
        public DbSet<Departamento> Departamentos => Set<Departamento>();
        public DbSet<Materia> Materias => Set<Materia>();
        public DbSet<Aula> Aulas => Set<Aula>();
        public DbSet<Presenca> Presencas => Set<Presenca>();
        public DbSet<Atribuicao> Atribuicoes => Set<Atribuicao>();
        public DbSet<AlunoDepartamento> AlunosDepartamentos => Set<AlunoDepartamento>();
        public DbSet<Parentesco> Parentescos => Set<Parentesco>();

        public override int SaveChanges()
        {
            AtualizarDatasAutomaticamente();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AtualizarDatasAutomaticamente();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void AtualizarDatasAutomaticamente()
        {
            var entradas = ChangeTracker.Entries<EntidadeBase>();

            foreach (var e in entradas)
            {
                if (e.State == EntityState.Added)
                    e.Entity.CriadoEm = DateTime.UtcNow;

                if (e.State == EntityState.Modified)
                    e.Entity.AtualizadoEm = DateTime.UtcNow;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
            modelBuilder.Entity<Parentesco>()
                .HasOne(p => p.Pessoa)
                .WithMany(p => p.Parentescos)
                .HasForeignKey(p => p.PessoaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Parentesco>()
                .HasOne(p => p.Parente)
                .WithMany(p => p.ParentescosComoParente)
                .HasForeignKey(p => p.ParenteId)
                .OnDelete(DeleteBehavior.Restrict);

            
            modelBuilder.Entity<Presenca>()
                .HasOne(p => p.Aula)
                .WithMany(a => a.Presencas)
                .HasForeignKey(p => p.AulaId);

            
            modelBuilder.Entity<AlunoDepartamento>()
                .HasIndex(m => new { m.DepartamentoId, m.PessoaId })
                .IsUnique()
                .HasFilter("\"Ativo\" = true");

            
            modelBuilder.Entity<Presenca>()
                .HasOne(p => p.AlunoDepartamento)
                .WithMany(m => m.Presencas)
                .HasForeignKey(p => p.AlunoDepartamentoId);

            
            modelBuilder.Entity<Presenca>()
                .HasIndex(p => new { p.AulaId, p.AlunoDepartamentoId })
                .IsUnique();

            
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();


        }
    }
}
