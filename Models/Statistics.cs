namespace AudienciasApp.Models
{
    public class Statistics
    {
        public int Realizadas { get; set; }
        public int NoRealizadas { get; set; }
        public int Total => Realizadas + NoRealizadas;
    }
}