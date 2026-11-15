namespace NoAzul.Application.BankConnections
{
    public static class BankCodes
    {
        public const string MeuPluggyConnectorName = "MeuPluggy";

        private static readonly Dictionary<string, string> Names = new()
        {
            ["001"] = "Banco do Brasil",
            ["033"] = "Santander",
            ["041"] = "Banrisul",
            ["070"] = "BRB",
            ["077"] = "Inter",
            ["104"] = "Caixa",
            ["208"] = "BTG Pactual",
            ["212"] = "Banco Original",
            ["237"] = "Bradesco",
            ["260"] = "Nubank",
            ["290"] = "PagBank",
            ["323"] = "Mercado Pago",
            ["336"] = "C6 Bank",
            ["341"] = "Itaú",
            ["380"] = "PicPay",
            ["422"] = "Safra",
            ["655"] = "Votorantim",
            ["735"] = "Neon",
            ["748"] = "Sicredi",
            ["756"] = "Sicoob"
        };

        public static (string Code, string Name)? FromTransferNumber(string? transferNumber)
        {
            if (string.IsNullOrWhiteSpace(transferNumber))
                return null;

            var digits = new string(transferNumber.Trim().TakeWhile(char.IsDigit).ToArray());

            if (digits.Length == 0 || digits.Length > 3)
                return null;

            var code = digits.PadLeft(3, '0');

            return Names.TryGetValue(code, out var name) ? (code, name) : null;
        }
    }
}
