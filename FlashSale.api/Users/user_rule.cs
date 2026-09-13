using OrderEventMessage = FlashSale.Api.OrderBook.OrderEvent.OrderEvent;

namespace Users.RuleBook;




public class RuleBook
{
    
    private readonly int PHASE1 = 1;

    private readonly int PHASE2 = 2;

    private readonly int PHASE3 = 3;

    public int curr_phase;

    //Default phase is 1
    public RuleBook()
    {
        curr_phase = 1;
    }
    

    public bool UserRulesValidation(List<OrderEventMessage> orders, int productId)
    {


        return true;
    }

}