using NoAzul.Domain.Enums.Transactions;

namespace NoAzul.Application.Transactions
{
    public static class PluggyCategoryMap
    {
        private static readonly string[] NotSpendingPrefixes =
        [
            "01",
            "03",
            "04",
            "0510"
        ];

        private static readonly (string Prefix, ETransactionCategory Category)[] Rules =
        [
            ("0701", ETransactionCategory.Subscriptions),
            ("0702", ETransactionCategory.Education),
            ("0703", ETransactionCategory.Health),
            ("0704", ETransactionCategory.Leisure),
            ("2002", ETransactionCategory.Home),
            ("2003", ETransactionCategory.Health),
            ("2004", ETransactionCategory.Transport),
            ("02", ETransactionCategory.Fees),
            ("08", ETransactionCategory.Shopping),
            ("09", ETransactionCategory.Subscriptions),
            ("10", ETransactionCategory.Groceries),
            ("11", ETransactionCategory.Restaurants),
            ("12", ETransactionCategory.Leisure),
            ("14", ETransactionCategory.Leisure),
            ("15", ETransactionCategory.Fees),
            ("16", ETransactionCategory.Fees),
            ("17", ETransactionCategory.Home),
            ("18", ETransactionCategory.Health),
            ("19", ETransactionCategory.Transport),
            ("21", ETransactionCategory.Leisure)
        ];

        public static bool IsSpending(string? providerCategoryId) =>
            string.IsNullOrEmpty(providerCategoryId)
            || !NotSpendingPrefixes.Any(prefix => providerCategoryId.StartsWith(prefix, StringComparison.Ordinal));

        public static ETransactionCategory ToCategory(string? providerCategoryId)
        {
            if (string.IsNullOrEmpty(providerCategoryId))
                return ETransactionCategory.Other;

            foreach (var (prefix, category) in Rules)
                if (providerCategoryId.StartsWith(prefix, StringComparison.Ordinal))
                    return category;

            return ETransactionCategory.Other;
        }
    }
}
