using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApolloLensLibrary.Utilities
{
    public static class ServerConfig
    {
        public static readonly string AwsAddress = "ws://apollosignaller-env.p47zti3ztv.us-east-2.elasticbeanstalk.com/";

        public static readonly string AzureAddress = "wss://apollosignalling.azurewebsites.net";

        public static readonly string LocalAddress = "ws://localhost:8080";
    }
}
