using System;

namespace CineApp
{
    public class FuncionInfo
    {
        public int FuncionId { get; set; }
        public string Pelicula { get; set; }
        public string Sala { get; set; }
        public DateTime FechaHoraInicio { get; set; }
        public string Display => $"{Pelicula} - {Sala} - {FechaHoraInicio:g}";
    }

    public class SeatStatus
    {
        public int AsientoId { get; set; }
        public string Asiento { get; set; }
        public int Ocupado { get; set; }
        public bool EstaOcupado => Ocupado == 1;
    }
}
