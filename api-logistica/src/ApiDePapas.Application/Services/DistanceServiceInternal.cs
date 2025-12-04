using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // Necesario para Task
using ApiDePapas.Application.Interfaces;
using ApiDePapas.Domain.Entities;
using ApiDePapas.Domain.Repositories;
using System.Text.RegularExpressions; // Para limpiar el CP

namespace ApiDePapas.Application.Services
{
    public class DistanceServiceInternal : IDistanceService
    {
        private readonly ILocalityRepository _locality_repository;

        public DistanceServiceInternal(ILocalityRepository localityRepository)
        {
            _locality_repository = localityRepository;
        }

        // Método auxiliar para limpiar el CP
        // Transforma "H3500AAA" -> "H3500" (Conserva la letra de provincia y los números)
        private string CleanPostalCode(string cpa)
        {
            if (string.IsNullOrEmpty(cpa)) return "";

            // Regex: ^[A-Za-z] busca una letra al inicio
            //        \d+      busca los números que le siguen
            // Esto capturará "H3500" e ignorará "AAA" del final.
            var match = Regex.Match(cpa, @"^[A-Za-z]\d+");

            if (match.Success)
            {
                return match.Value.ToUpper(); // Retorna H3500 en mayúsculas
            }
            return Regex.Replace(cpa, @"\s+", "").ToUpper(); // Si no coincide, retorna el CP sin espacios en mayúsculas
        }

        public (double lat, double lon) GetAverageCoordinates(List<(double, double)> points)
        {
            if (points == null || !points.Any())
            {
                // Retornar 0,0 en lugar de lanzar error para ser más resiliente
                return (0, 0);
            }

            double x = 0.0;
            double y = 0.0;
            double z = 0.0;
            int count = 0;

            foreach (var (latDeg, lonDeg) in points)
            {
                double latRad = DegreesToRadians(latDeg);
                double lonRad = DegreesToRadians(lonDeg);
                double cosLat = Math.Cos(latRad);

                x += cosLat * Math.Cos(lonRad);
                y += cosLat * Math.Sin(lonRad);
                z += Math.Sin(latRad);
                count++;
            }

            x /= count;
            y /= count;
            z /= count;

            double lonCenter = Math.Atan2(y, x);
            double hyp = Math.Sqrt(x * x + y * y);
            double latCenter = Math.Atan2(z, hyp);

            return (RadiansToDegrees(latCenter), RadiansToDegrees(lonCenter));
        }

        public async Task<double> GetDistanceKm(string originCpa, string destinationCpa)
        {
            // 1. Limpiamos los códigos postales para buscar solo los números (ej. "3500")
            string cleanOrigin = CleanPostalCode(originCpa);
            string cleanDest = CleanPostalCode(destinationCpa);

            // 2. CORRECCIÓN CLAVE: Usamos 'await' en lugar de '.Result'
            List<Locality> possibleOriginLocalities = await _locality_repository.GetByPostalCodeAsync(cleanOrigin);
            List<Locality> possibleDestinationLocalities = await _locality_repository.GetByPostalCodeAsync(cleanDest);

            // 3. Verificamos si encontramos localidades
            if (!possibleOriginLocalities.Any() || !possibleDestinationLocalities.Any()) 
            { 
                // Si no encontramos nada, intentamos buscar sin limpiar (por si la BD sí tiene letras)
                if (!possibleOriginLocalities.Any()) 
                    possibleOriginLocalities = await _locality_repository.GetByPostalCodeAsync(originCpa);
                
                if (!possibleDestinationLocalities.Any())
                    possibleDestinationLocalities = await _locality_repository.GetByPostalCodeAsync(destinationCpa);

                // Si seguimos sin encontrar, devolvemos fallback
                if (!possibleOriginLocalities.Any() || !possibleDestinationLocalities.Any())
                    return 300.0; 
            }

            List<(double lat, double lon)> possibleOriginCoords = possibleOriginLocalities
                .Select(l => ((double)l.lat, (double)l.lon))
                .ToList();

            List<(double lat, double lon)> possibleDestinationCoords = possibleDestinationLocalities
                .Select(l => ((double)l.lat, (double)l.lon))
                .ToList();

            var originCentroid = GetAverageCoordinates(possibleOriginCoords);
            var destinationCentroid = GetAverageCoordinates(possibleDestinationCoords);

            return HaversineKm(originCentroid.lat, originCentroid.lon, destinationCentroid.lat, destinationCentroid.lon);
        }

        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            double dLat = DegreesToRadians(lat2 - lat1);
            double dLon = DegreesToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double DegreesToRadians(double degrees) => degrees * (double)Math.PI / 180.0f;
        private static double RadiansToDegrees(double radians) => radians * 180.0f / (double)Math.PI;
    }
}