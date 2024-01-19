using MNet;

Client client = new Client("127.0.0.1", 12345);
client.Connect();
while(client.IsConnected)
{
    client.UpdateMessage();
}