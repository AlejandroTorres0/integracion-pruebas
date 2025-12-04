using ApiDePapas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection.Metadata;

namespace ApiDePapas.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // 1. DbSet para las Entidades Principales (Tablas)
        public DbSet<ShippingDetail> Shippings { get; set; } = null!;
        public DbSet<DistributionCenter> DistributionCenters { get; set; } = null!;
        public DbSet<TransportMethod> TransportMethods { get; set; } = null!;
        public DbSet<Travel> Travels { get; set; } = null!;
        public DbSet<Locality> Localities { get; set; } = null!;
        public DbSet<Address> Addresses { get; set; } = null!; // Address es ahora una tabla

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 2. Definición de Claves Primarias
            modelBuilder.Entity<ShippingDetail>().HasKey(s => s.shipping_id);
            modelBuilder.Entity<TransportMethod>().HasKey(t => t.transport_id);
            modelBuilder.Entity<DistributionCenter>().HasKey(dc => dc.distribution_center_id);
            modelBuilder.Entity<Travel>().HasKey(t => t.travel_id);
            modelBuilder.Entity<Address>().HasKey(a => a.address_id); // PK de Address
            modelBuilder.Entity<Locality>().HasKey(l => new { l.postal_code, l.locality_name }); // Clave Compuesta

            // 3. Mapeo de Relaciones de Entidades

            // A. Relación ShippingDetail (N) a Travel (1)
            modelBuilder.Entity<ShippingDetail>()
                .HasOne(s => s.Travel)
                .WithMany(t => t.Shippings)
                .HasForeignKey(s => s.travel_id)
                .IsRequired();

            // B. Relación Travel (N) a TransportMethod (1)
            modelBuilder.Entity<Travel>()
                .HasOne(t => t.TransportMethod)
                .WithMany()
                .HasForeignKey(t => t.transport_method_id)
                .IsRequired();

            // C. Relación Travel (N) a DistributionCenter (1)
            modelBuilder.Entity<Travel>()
                .HasOne(t => t.DistributionCenter)
                .WithMany()
                .HasForeignKey(t => t.distribution_center_id)
                .IsRequired();

            // D. Relación ShippingDetail (N) a Address (1) - SOLO ENTREGA
            modelBuilder.Entity<ShippingDetail>()
                .HasOne(s => s.DeliveryAddress) // Propiedad de navegación 'DeliveryAddress'
                .WithMany(a => a.DeliveredShippings) // Colección en Address
                .HasForeignKey(s => s.delivery_address_id) // Clave foránea en ShippingDetail
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            // E. Relación Address (N) a Locality (1)
            modelBuilder.Entity<Address>()
                .HasOne(a => a.Locality) // Address tiene UNA Locality
                .WithMany() // Locality no necesita navegar de vuelta a Address
                .HasForeignKey(a => new { a.postal_code, a.locality_name }) // Usa la FK compuesta
                .IsRequired();

            // 4. Mapeo de Propiedades Secundarias

            // A. Colecciones (Tablas Satélite)
            // --- MODIFICADO --- (Añadido HasIndex)
            modelBuilder.Entity<ShippingDetail>().OwnsMany(s => s.products, owned =>
            {
                owned.ToTable("ProductQty");
                owned.Property<int>("RowId").ValueGeneratedOnAdd();
                owned.HasKey("RowId", "ShippingDetailshipping_id");

                owned.WithOwner()
                     .HasForeignKey("ShippingDetailshipping_id");

                // --- NUEVO ---
                // Indexamos la FK para acelerar la carga de productos
                owned.HasIndex("ShippingDetailshipping_id")
                     .HasDatabaseName("IX_ProductQty_ShippingDetailshipping_id");
            });

            // --- MODIFICADO --- (Añadido HasIndex)
            modelBuilder.Entity<ShippingDetail>().OwnsMany(s => s.logs, owned =>
            {
                // --- NUEVO ---
                // Asumimos que la tabla se llama 'ShippingLog'
                // y que la FK se llama 'ShippingDetailshipping_id'
                // ¡Ajusta esto si tus logs se configuran diferente!
                owned.ToTable("ShippingLog");

                // Indexamos la FK para acelerar la carga de logs
                owned.HasIndex("ShippingDetailshipping_id")
                     .HasDatabaseName("IX_ShippingLog_ShippingDetailshipping_id");
            });

            // B. Dirección dentro de DistributionCenter (como Value Object)
            modelBuilder.Entity<DistributionCenter>()
                    .HasOne(dc => dc.Address) // Centro tiene UNA Dirección
                    .WithMany() // No necesitamos navegar de vuelta desde Address a DistributionCenter
                    .HasForeignKey(dc => dc.address_id)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

            // 5. Mapeo necesario para TransportMethods
            modelBuilder.Entity<TransportMethod>()
                    .Property(t => t.transport_type)
                    .HasConversion<string>();


            // --- INICIO DE LA SECCIÓN COMPLETAMENTE NUEVA ---

            // 6. Definición de Índices para Optimización de Rendimiento
            // (Estos no definen relaciones, solo aceleran las consultas)

            // A. Índices para ShippingDetail (asumo que tu clase se llama así)
            modelBuilder.Entity<ShippingDetail>(entity =>
            {
                // Para acelerar JOINs
                entity.HasIndex(s => s.travel_id)
                      .HasDatabaseName("IX_Shippings_travel_id");

                entity.HasIndex(s => s.delivery_address_id)
                      .HasDatabaseName("IX_Shippings_delivery_address_id");

                // Para acelerar WHERE (Asumo que tienes estas propiedades)
                // ¡Borra o edita estas líneas si tus propiedades se llaman diferente!
                entity.HasIndex(s => s.status)
                      .HasDatabaseName("IX_Shippings_status");

                entity.HasIndex(s => s.created_at)
                      .HasDatabaseName("IX_Shippings_created_at");
            });

            // B. Índices para Travel
            modelBuilder.Entity<Travel>(entity =>
            {
                // Para acelerar JOINs
                entity.HasIndex(t => t.transport_method_id)
                      .HasDatabaseName("IX_Travels_transport_method_id");

                entity.HasIndex(t => t.distribution_center_id)
                      .HasDatabaseName("IX_Travels_distribution_center_id");
            });

            // C. Índice para Address
            modelBuilder.Entity<Address>(entity =>
            {
                // Para acelerar el JOIN compuesto con Locality
                entity.HasIndex(a => new { a.postal_code, a.locality_name })
                      .HasDatabaseName("IX_Addresses_locality_fk");
            });

            // D. Índice para DistributionCenter
            modelBuilder.Entity<DistributionCenter>(entity =>
            {
                // Para acelerar JOIN con Address
                entity.HasIndex(dc => dc.address_id)
                      .HasDatabaseName("IX_DistributionCenters_address_id");
            });

            // E. Índice para Locality
            modelBuilder.Entity<Locality>(entity =>
            {
                // Para acelerar búsquedas de texto (WHERE locality_name = '...')
                entity.HasIndex(l => l.locality_name)
                      .HasDatabaseName("IX_Localities_locality_name");
            });

            // --- FIN DE LA SECCIÓN COMPLETAMENTE NUEVA ---


            base.OnModelCreating(modelBuilder);
        }
    }
}
