using KSeF.Client;
using KSeF.Client.DI;
using Microsoft.Extensions.DependencyInjection;

var sc = new ServiceCollection();
sc.AddKSeFClient(opts => { opts.BaseUrl = KsefEnviromentsUris.TEST; opts.CustomHeaders = []; });
using var sp = sc.BuildServiceProvider();
var ksefClient = sp.GetRequiredService<IKSeFClient>();

Console.WriteLine("Ok");