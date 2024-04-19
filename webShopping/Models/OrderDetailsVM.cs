namespace webShopping.Models
{
    public class OrderDetailsVM
    {
        public OrderHeader OrderHeader { get; set; }
        public IEnumerable<orderDetails> orderDetails { get; set; }
    }
}
