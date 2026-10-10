
using OrderMessage.OrderEvent;
using Service.ServicesHandler;
using Sylvan.Data;
using Sylvan.Data.Csv;




public class OrderWriter: ServiceHandler
{
    
    string _filepath = "./Data/Orders.csv";
    
    StreamWriter _writer;

    public OrderWriter()
    {
          Directory.CreateDirectory(
            Path.GetDirectoryName(Path.GetFullPath(_filepath))!
        );
        
        _writer = new StreamWriter(_filepath, append: false);

        // write line
         _writer.WriteLine(
            "OrderId,UserId,ProductId,Quantity,Price,Timestamp,Status"
        );
    }

    public ValueTask HandleAsync(OrderEvent data, CancellationToken cancellationToken)
    {
        _writer.WriteLine(string.Join(",",
                data._orderId,
                data._userId,
                data._productId,
                data._qty,
                data._price,
                data._timeStamp,
                data._state
            ));
      
        return ValueTask.CompletedTask;
    }

  
    
}