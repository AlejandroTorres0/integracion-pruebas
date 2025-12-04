using ApiDePapas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection; // <-- CAMBIO: Ya no usamos AspNetCore.Builder
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Data;

namespace ApiDePapas.Infrastructure.Persistence
{
    public static class DatabaseInitializer
    {
        // --- INICIO DEL CAMBIO ---
        // Ya no es un método de extensión 'this WebApplication app'
        // Ahora es un método estático normal que recibe el proveedor de servicios.
        public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
        // --- FIN DEL CAMBIO ---
        {
            // Creamos un scope desde el proveedor de servicios que nos pasaron
            using (var scope = serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                // Usamos ILogger<DatabaseInitializer> para que sea más específico
                var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("DatabaseInitializer"); 

                try
                {
                    var dbContext = services.GetRequiredService<ApplicationDbContext>();

                    logger.LogInformation("Conectando a la base de datos y aplicando migraciones...");
                    await dbContext.Database.MigrateAsync();
                    logger.LogInformation("Migraciones aplicadas con éxito.");

                    if (!await dbContext.Localities.AnyAsync())
                    {
                        // ... (El resto del método 'try...catch' es EXACTAMENTE IGUAL) ...
                        // (No es necesario copiarlo todo, solo cambia la firma del método
                        //  y los 'usings' de arriba)
                        logger.LogInformation("Base de datos vacía. Iniciando carga masiva de datos...");

                        var connection = dbContext.Database.GetDbConnection() as MySqlConnection;
                        if (connection == null)
                        {
                            logger.LogError("No se pudo obtener la conexión MySqlConnector.");
                            return;
                        }

                        if (connection.State != ConnectionState.Open)
                        {
                            await connection.OpenAsync();
                        }

                        await dbContext.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;");
                        await dbContext.Database.ExecuteSqlRawAsync(@"
                            TRUNCATE TABLE ProductQty;
                            TRUNCATE TABLE ShippingLog;
                            TRUNCATE TABLE Shippings;
                            TRUNCATE TABLE Travels;
                            TRUNCATE TABLE DistributionCenters;
                            TRUNCATE TABLE TransportMethods;
                            TRUNCATE TABLE Addresses;
                            TRUNCATE TABLE Localities;
                        ");

                        await LoadCsvDataAsync(connection, logger, "/app/csvs/_locality.csv", "Localities", ';',
                            "postal_code", "locality_name", "state_name", "country", "lat", "lon");

                        await LoadCsvDataAsync(connection, logger, "/app/csvs/_transport_method_truck.csv", "TransportMethods", ';',
                            "transport_id", "transport_type", "average_speed", "available", "max_capacity");
                        await LoadCsvDataAsync(connection, logger, "/app/csvs/_transport_method_boat.csv", "TransportMethods", ';',
                            "transport_id", "transport_type", "average_speed", "available", "max_capacity");
                        await LoadCsvDataAsync(connection, logger, "/app/csvs/_transport_method_plane.csv", "TransportMethods", ';',
                            "transport_id", "transport_type", "average_speed", "available", "max_capacity");
                        await LoadCsvDataAsync(connection, logger, "/app/csvs/_transport_method_train.csv", "TransportMethods", ';',
                            "transport_id", "transport_type", "average_speed", "available", "max_capacity");

                        await LoadCsvDataAsync(connection, logger, "/app/csvs/_addresses.csv", "Addresses", ',',
                            "address_id", "street", "number", "postal_code", "locality_name");

                        await LoadCsvDataAsync(connection, logger, "/app/csvs/_distribution_centers.csv", "DistributionCenters", ',',
                            "distribution_center_id", "address_id");

                        await LoadCsvDataAsync(connection, logger, "/app/csvs/_travels.csv", "Travels", ',',
                            "travel_id", "departure_time", "arrival_time", "transport_method_id", "distribution_center_id");

                        await LoadCsvDataAsync(connection, logger, "/app/csvs/_shipping.csv", "Shippings", ',',
                            "shipping_id", "order_id", "user_id", "delivery_address_id", "status", "travel_id", "tracking_number", "carrier_name", "total_cost", "currency", "estimated_delivery_at", "created_at", "updated_at");

                        await LoadCsvDataAsync(connection, logger, "/app/csvs/_productqty.csv", "ProductQty", ',',
                            "id", "ShippingDetailshipping_id", "quantity");
                        
                        await dbContext.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;");

                        logger.LogInformation("Carga masiva de datos completada.");
                    }
                    else
                    {
                        logger.LogInformation("La base de datos ya contiene datos. Omitiendo carga masiva.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ocurrió un error grave al inicializar la base de datos.");
                    Environment.Exit(1);
                }
            }
        }

        private static async Task LoadCsvDataAsync(MySqlConnection connection, ILogger logger, string filePath, string tableName, char fieldTerminator, params string[] columnNames)
        {
            logger.LogInformation("Cargando {TableName} desde {FilePath}...", tableName, filePath);
            try
            {
                var bulkLoader = new MySqlBulkLoader(connection)
                {
                    FileName = filePath,
                    TableName = tableName,
                    FieldTerminator = fieldTerminator.ToString(),
                    LineTerminator = "\n",
                    NumberOfLinesToSkip = 1,
                    Local = true
                };
                bulkLoader.Columns.AddRange(columnNames);

                var rows = await bulkLoader.LoadAsync();
                logger.LogInformation("Cargados {Rows} registros en {TableName}.", rows, tableName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al cargar {TableName} desde {FilePath}", tableName, filePath);
                throw;
            }
        }
    }
}