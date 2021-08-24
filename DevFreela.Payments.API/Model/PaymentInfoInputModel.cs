namespace DevFreela.Payments.API.Model
{
    public class PaymentInfoInputModel
    {
        /// <summary>
        /// Identifica ao qual projeto se refere
        /// </summary>
        public int IdProjeto { get; set; }
        public string CreditCardNumber { get; set; }
        public int Cvv { get; set; }
        public string ExpiresAt { get; set; }
        public string FullName { get; set; }
        public decimal Amount { get; set; }
    }
}
