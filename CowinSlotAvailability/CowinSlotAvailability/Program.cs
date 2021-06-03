using System;
using System.Data;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Net.Mail;
using System.Configuration;
using System.Threading;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            string apiHitIterations = ConfigurationManager.AppSettings["apiHitIterations"].ToString();
            int counter = 1;
            while (counter <= Convert.ToInt32(apiHitIterations))
            {
                try
                {
                    string toEmailIdAddress = ConfigurationManager.AppSettings["toEmailIdAddress"].ToString();
                    string fromEmailIdAddress = ConfigurationManager.AppSettings["fromEmailIdAddress"].ToString();
                    string fromEmailIdPassword = ConfigurationManager.AppSettings["fromEmailIdPassword"].ToString();
                    string districtIDs = ConfigurationManager.AppSettings["districtIDs"].ToString();
                    string NextNoOfDays = ConfigurationManager.AppSettings["NextNoOfDays"].ToString();

                    DataTable vcAvailable = new DataTable("vaccine");
                    vcAvailable.Columns.Add("Date", typeof(String));
                    vcAvailable.Columns.Add("AvailableDose", typeof(String));
                    vcAvailable.Columns.Add("CentreName", typeof(String));
                    vcAvailable.Columns.Add("Pin", typeof(String));
                    vcAvailable.Columns.Add("District", typeof(String));
                    vcAvailable.Columns.Add("Age", typeof(String));
                    vcAvailable.Columns.Add("Vaccine", typeof(String));
                    vcAvailable.Columns.Add("State", typeof(String));
                    vcAvailable.Columns.Add("Block", typeof(String));
                    vcAvailable.Columns.Add("FeeType", typeof(String));

                    //refer mapping.csv to get your district id                    

                    string[] distIds = districtIDs.Split(',');

                    foreach (string distId in distIds)
                    {
                        for (int j = 0; j < Convert.ToInt32(NextNoOfDays); j++)
                        {
                            ServicePointManager.Expect100Continue = true;
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;
                            string dt = (DateTime.Now.AddDays(j)).ToString("dd-MM-yyyy");
                            var apiUrl = "https://cdn-api.co-vin.in/api/v2/appointment/sessions/public/findByDistrict?district_id=" + distId + "&date=" + dt;

                            var request = (HttpWebRequest)WebRequest.Create(apiUrl);
                            request.Method = "GET";
                            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.93 Safari/537.36";

                            var content = string.Empty;
                            using (var response = (HttpWebResponse)request.GetResponse())
                            {
                                using (var stream = response.GetResponseStream())
                                {
                                    using (var sr = new StreamReader(stream))
                                    {
                                        content = sr.ReadToEnd();

                                        var dynamicobject = JsonConvert.DeserializeObject<dynamic>(content);

                                        if (dynamicobject.sessions.Count > 0)
                                        {
                                            Console.WriteLine("Getting Response for Url " + apiUrl);

                                            for (int k = 0; k < dynamicobject.sessions.Count; k++)
                                            {
                                                String availabledose = dynamicobject.sessions[k].available_capacity_dose1.ToString();
                                                String agechk = dynamicobject.sessions[k].min_age_limit.ToString();
                                                if ((Convert.ToInt32(availabledose) > 1) && (agechk == "18"))
                                                {
                                                    DataRow newrow = vcAvailable.NewRow();
                                                    newrow["Date"] = dynamicobject.sessions[k].date.ToString();
                                                    newrow["AvailableDose"] = dynamicobject.sessions[k].available_capacity_dose1.ToString();
                                                    newrow["CentreName"] = dynamicobject.sessions[k].name.ToString();
                                                    newrow["Pin"] = dynamicobject.sessions[k].pincode.ToString();
                                                    newrow["District"] = dynamicobject.sessions[k].district_name.ToString();
                                                    newrow["Age"] = dynamicobject.sessions[k].min_age_limit.ToString();
                                                    newrow["Vaccine"] = dynamicobject.sessions[k].vaccine.ToString();
                                                    newrow["State"] = dynamicobject.sessions[k].state_name.ToString();
                                                    newrow["Block"] = dynamicobject.sessions[k].block_name.ToString();
                                                    newrow["FeeType"] = dynamicobject.sessions[k].fee_type.ToString();
                                                    vcAvailable.Rows.Add(newrow);

                                                    Console.WriteLine("Response" + content.ToString());
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("No Response for Url " + apiUrl);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (vcAvailable.Rows.Count > 0)
                    {

                        //Enter to email on which you want to receive notifications
                        object toEmailVcGroup = toEmailIdAddress;

                        string emailsubject = "ALERT: Vaccine Availablity | " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
                        string emailbody = "<table border=" + 1 + " cellpadding=" + 1 + " cellspacing=" + 0 + " width = " + 1000 + "><tr bgcolor='#8AFF33'><td align='center'><b><font size=4>  Date </b></td><td align='center'><b><font size=4>  Available Dose </b></td><td align='center'><b><font size=4> Centre Name </b></td><td align='center'><b><font size=4>  Pincode </b></td><td align='center'><b><font size=4>  District </b></td><td align='center'><b><font size=4>  AGE </b></td><td align='center'><b><font size=4>  Vaccine </b></td><td align='center'><b><font size=4>  State </b></td><td align='center'><b><font size=4>  Block </b></td><td align='center'><b><font size=4>  FeeType </b></td></tr>";
                        string emailto = toEmailVcGroup.ToString();

                        foreach (DataRow dtRow in vcAvailable.Rows)
                        {
                            emailbody += "<tr><td align='center'><font size=4>" + dtRow["Date"].ToString() + "</td><td align='center'><font size=4>" + dtRow["AvailableDose"].ToString() + "</td><td align='center'><font size=4>" + dtRow["CentreName"].ToString() + "</td><td align='center'><font size=4>" + dtRow["Pin"].ToString() + "</td><td align='center'><font size=4>" + dtRow["District"].ToString() + "</td><td align='center'><font size=4>" + dtRow["Age"].ToString() + "</td><td align='center'><font size=4>" + dtRow["Vaccine"].ToString() + "</td><td align='center'><font size=4>" + dtRow["State"].ToString() + "</td><td align='center'><font size=4>" + dtRow["Block"].ToString() + "</td><td align='center'><font size=4>" + dtRow["FeeType"].ToString() + "</td></tr>";
                        }
                        emailbody += "</table>";

                        MailMessage message = new MailMessage();

                        message.To.Add(new MailAddress(fromEmailIdAddress));

                        string[] ToMuliId = emailto.Split(',');
                        foreach (string ToEMailId in ToMuliId)
                        {
                            message.Bcc.Add(new MailAddress(ToEMailId));
                        }
                        message.From = new MailAddress(fromEmailIdAddress, "Vaccine");
                        message.Subject = emailsubject;
                        message.Body = emailbody;
                        message.IsBodyHtml = true;
                        using (var smtp = new SmtpClient())
                        {
                            var credential = new NetworkCredential
                            {
                                UserName = fromEmailIdAddress,  // replace with valid value
                                Password = fromEmailIdPassword  // replace with valid value
                            };
                            smtp.Credentials = credential;
                            smtp.Host = "smtp.gmail.com";
                            smtp.Port = 587;
                            smtp.EnableSsl = true;
                            smtp.Send(message);
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception " + e.ToString());
                }
                Console.WriteLine("Iteration   " + counter);
                counter = counter + 1;
                if (counter <= Convert.ToInt32(apiHitIterations))
                {
                    //Add Delay to avoid ip block
                    Thread.Sleep(10000);
                }
            }
        }
    }
}
