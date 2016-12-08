using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace sensibo.sensibo
{

    class restclient
    {
        const string hosturl = "home.sensibo.com";
        const string basePath = "/api/v2";
        const string schemes = "https";
        const string consumer = "application/json";
        const string producer = "application/json; charset=utf-8";
        string apikey = ""; //Enter you apikey here if you want to hard code it

        public restclient(string apiKey)
        {
            apikey = apiKey;
        }

        #region GET methods
        /// <summary>
        /// Gets all pods registered within the specified account.
        /// </summary>
        /// <returns>Returns a <see cref="pods"/> containing all pods returned by the API.</returns>
        public pods getpods()
        {

            return getdata<pods>("/users/me/pods?fields=id,room");

        }

        /// <summary>
        /// Gets the status of the pod identified by its id.
        /// </summary>
        /// <param name="id">The id of the pod to read its status from.</param>
        /// <returns>Returns a <see cref="acstatus"/> indicating the current status of the selected pod.</returns>
        public acstatus getpodstatus(string id)
        {
            return getdata<acstatus>(String.Format("/pods/{0}/acStates?fields=status,acState&limit=1", id));
        }

        /// <summary>
        /// Gets the measurements of the pod identified by its id.
        /// </summary>
        /// <param name="id">The id of the pod to read its measurements from.</param>
        /// <returns>Returns a <see cref="measurements"/> containing all the measurements from the selected pod.</returns>
        public measurements getpodmeasurements(string id)
        {
            return getdata<measurements>(String.Format("/pods/{0}/measurements", id));
        }
        #endregion

        #region POST methods
        /// <summary>
        /// Updates the state of the selected pod identified by its id.
        /// </summary>
        /// <param name="id">The id of the pod to have its state updated.</param>
        /// <param name="targetstate">The new state the pod should assume.</param>
        /// <returns>Returns a <see cref="setResult"/> to indicate whether the command was successful or not.</returns>
        public setResult postpodstatus(string id, SetAcState targetstate)
        {

            JsonAcState Jstate = new JsonAcState();
            Jstate.acState = targetstate;

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(Jstate);

            var request = this.createrequest(String.Format("/pods/{0}/acStates", id), "POST", producer);

            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }

            return handleresponse<setResult>(request);

        }
        #endregion

        #region Helpers
        /// <summary>
        /// Creates an Http Request and sends a GET command to the path specified.
        /// </summary>
        /// <typeparam name="T">The type that will be returned by the GET command.;</typeparam>
        /// <param name="datapath">The path to GET the data from.</param>
        /// <returns>Returns a strongly typed object converted from the specified request.</returns>
        private T getdata<T>(string datapath)
        {
            HttpWebRequest request = createrequest(datapath, "GET", consumer);

            return handleresponse<T>(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <returns></returns>
        private static T handleresponse<T>(HttpWebRequest request)
        {
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                var responseValue = string.Empty;

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var message = String.Format("Request failed. Received HTTP {0}", response.StatusCode);
                    throw new ApplicationException(message);
                }

                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                        using (var reader = new StreamReader(responseStream))
                        {
                            responseValue = reader.ReadToEnd();
                        }
                }

                //Convert the json respons to the requested object
                T data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(responseValue);
                return data;
            }
        }

        /// <summary>
        /// Creates an Http Request and returns it.
        /// </summary>
        /// <param name="datapath">The path to create the request from.</param>
        /// <param name="method">The HTTP method to use. Available methods are: 'GET' and 'POST'.</param>
        /// <param name="contenttype">The content type of the request. For GET commands use <see cref="consumer"/> and for POST commands use <see cref="producer"/>.</param>
        /// <returns>Returns an Http Request using the specified method and datapath.</returns>
        private HttpWebRequest createrequest(string datapath, string method, string contenttype)
        {

            var delim = datapath.IndexOf('?') >= 0 ? '&' : '?';
            var request = (HttpWebRequest)WebRequest.Create(
                String.Format("{0}://{1}{2}{3}{4}apiKey={5}", schemes, hosturl, basePath, datapath, delim, apikey)
            );
            request.Method = method;
            request.ContentType = contenttype;

            return request;

        }
        #endregion
    }
}
