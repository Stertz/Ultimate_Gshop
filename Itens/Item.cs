using Avalonia.Media.Imaging;
using Ultimate_Gshop.Misc;

namespace Ultimate_Gshop.Itens
{
    public class Item
    {
        public int ID { get; set; }
        public int Amount { get; set; }
        public int Place { get; set; }
        public int CategoryIndex { get; set; }
        public int SubCategoryIndex { get; set; }
        public string Icon { get; set; } = string.Empty;
        public List<Sale> Sales { get; set; } = [];
        public string Explanation { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int GiftID { get; set; }
        public int GiftAmount { get; set; }
        public int GiftTime { get; set; }
        public int ILogPrice { get; set; }
        public int[] OwnerNpcs { get; set; } = [];
        public int LimitPeriod { get; set; }
        public int AvailFrequency { get; set; }
        public int Class { get; set; }
        public int Reserve1 { get; set; }
        public int Reserve2 { get; set; }
        public int Reserve3 { get; set; }
        public int Reserve4 { get; set; }
        public int Reserve5 { get; set; }
        public int Reserve6 { get; set; }

        public Item Clone()
        {
            return new()
            {
                Place = Place,
                CategoryIndex = CategoryIndex,
                SubCategoryIndex = SubCategoryIndex,
                Icon = Icon,
                ID = ID,
                Amount = Amount,
                Sales = [.. Sales.Select(s => s.Clone())],
                Explanation = Explanation,
                Name = Name,
                GiftID = GiftID,
                GiftAmount = GiftAmount,
                GiftTime = GiftTime,
                ILogPrice = ILogPrice,
                OwnerNpcs = (int[])OwnerNpcs.Clone(),
                LimitPeriod = LimitPeriod,
                AvailFrequency = AvailFrequency,
                Class = Class,
                Reserve1 = Reserve1,
                Reserve2 = Reserve2,
                Reserve3 = Reserve3,
                Reserve4 = Reserve4,
                Reserve5 = Reserve5,
                Reserve6 = Reserve6
            };
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Item);
        }

        public bool Equals(Item? other)
        {
            if (other is null)
                return false;

            return this.ID == other.ID;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public Bitmap LoadImage()
        {
            return ImageUtils.LoadImage(Icon);
        }

        public Bitmap LoadImageReduced()
        {
            return ImageUtils.LoadImageReduced(Icon, 32, 32);
        }

        /// <summary>
        /// Retorna o preço válido de um item (convertido para decimal),
        /// baseado em regras de status e disponibilidade.
        /// - Prioriza Sales com Status ativo.
        /// - Verifica SellingEndTime (ignora vendas expiradas).
        /// - Considera Day como dia fixo de disponibilidade (se configurado).
        /// - Divide por 100 para retornar o valor monetário real.
        /// </summary>
        /// <param name="sales">Lista de opções de venda (Sales).</param>
        /// <returns>Preço válido como decimal ou 0 se nenhum for encontrado.</returns>
        public decimal GetValidPrice()
        {
            var sales = this.Sales;
            if (sales == null || sales.Count == 0)
                return 0m;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int today = (int)DateTime.UtcNow.DayOfWeek; // Caso 'Day' represente dia da semana

            // Filtra vendas válidas com base em status e tempo
            var validSales = sales.Where(s =>
                (s.SellingEndTime == 0 || s.SellingEndTime > now) && // Ainda disponível
                (s.Day == 0 || s.Day == today)                       // Se Day for usado, confere
            ).ToList();

            if (validSales.Count == 0)
                return 0m;

            // Prioriza vendas com status ativo > 0
            var activeSale = validSales.FirstOrDefault(s => s.Status > 0);
            var price = (activeSale ?? validSales.First()).Price;

            // Converte para decimal e divide por 100
            return price / 100m;
        }
    }
}
