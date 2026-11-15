using NoAzul.Domain.Enums.Transactions;

namespace NoAzul.Application.Transactions
{
    public static class CategoryKeys
    {
        private const string CustomPrefix = "custom-";

        public static string For(ETransactionCategory category, int? customCategoryId) =>
            customCategoryId.HasValue
                ? Custom(customCategoryId.Value)
                : char.ToLowerInvariant(category.ToString()[0]) + category.ToString()[1..];

        public static string Custom(int customCategoryId) => CustomPrefix + customCategoryId;

        public static bool TryParse(string? value, out ETransactionCategory category, out int? customCategoryId)
        {
            category = ETransactionCategory.Other;
            customCategoryId = null;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var trimmed = value.Trim();

            if (trimmed.StartsWith(CustomPrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(trimmed[CustomPrefix.Length..], out var id) || id <= 0)
                    return false;

                customCategoryId = id;
                return true;
            }

            return !int.TryParse(trimmed, out _)
                && Enum.TryParse(trimmed, ignoreCase: true, out category)
                && Enum.IsDefined(category);
        }
    }
}
