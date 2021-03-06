namespace DevFreela.Payments.API.Model
{
    public class PaymentInfoInputModel
    {
        /// <summary>
        /// Identifica ao qual projeto se refere
        /// </summary>
        public int IdProject { get; set; }
        public string CreditCardNumber { get; set; }
        public string Cvv { get; set; }
        public string ExpiresAt { get; set; }
        public string FullName { get; set; }
        public decimal Amount { get; set; }
    }
}
