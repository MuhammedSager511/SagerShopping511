namespace webShopping.Models
{
    public class ShoppingCartVM
    {
        public IEnumerable<ShoppingCart> ListCart { get; set; } = [];
        public OrderHeader OrderHeader { get; set; } = new();
        public bool AcceptTerms { get; set; }
    }
}
