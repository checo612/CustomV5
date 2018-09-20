using Acr.UserDialogs;
using Newtonsoft.Json;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using CustomV5.Models;
using static CustomV5.Models.ImageTextModel;
using Region = CustomV5.Models.ImageTextModel.Region;
using static CustomV5.Models.PredictionResponseModel;

namespace CustomV5
{
    public partial class MainPage : ContentPage
    {
        private MediaFile _foto;
        private DniModel dniModel;
        public MainPage()
        {
            InitializeComponent();
        }

        private async void ElegirClick(object sender, EventArgs e)
        {
            using (UserDialogs.Instance.Loading("Loading..."))
            {
                await CrossMedia.Current.Initialize();

                var foto = await CrossMedia.Current.PickPhotoAsync(new PickMediaOptions()
                {
                    CompressionQuality = 100
                });

                if (foto == null)
                {
                    return;
                }

                _foto = foto;
                ImgSource.Source = FileImageSource.FromFile(foto.Path);
            }
        }

        private async void TomarClick(object sender, EventArgs e)
        {
            using (UserDialogs.Instance.Loading("Loading..."))
            {
                await CrossMedia.Current.Initialize();

                var foto = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions()
                {
                    CompressionQuality = 100,
                    SaveToAlbum = true
                    //Directory="clasificator",
                    //Name="source.jpg"
                });

                if (foto == null)
                {
                    return;
                }

                _foto = foto;
                ImgSource.Source = FileImageSource.FromFile(foto.Path);

            }
        }

        private async void ClasificadorClick(object sender, EventArgs e)
        {
            const string endpoint = "https://southcentralus.api.cognitive.microsoft.com/customvision/v2.0/Prediction/f0bbc42f-ca2d-4c55-b66d-c81536c51972/image?iterationId=4c950f4f-0e75-4292-80f5-675a52688a3c";
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Prediction-Key", "d20c03142343439d8598d1cf03558421");

            var contentStream = new StreamContent(_foto.GetStream());

            using (UserDialogs.Instance.Loading("Loading..."))
            {
                var response = await httpClient.PostAsync(endpoint, contentStream);

                if (!response.IsSuccessStatusCode)
                {
                    UserDialogs.Instance.Toast("Un error a ocurrido");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();

                var prediction = JsonConvert.DeserializeObject<PredictionResponse>(json);

                var tag = prediction.predictions.First();

                Resultado.Text = $"{tag.tagName} - {tag.probability:p0}";
                Precision.Progress = tag.probability;
            }
        }

        private async void AnalizarTexto(object sender, EventArgs e)
        {
            var httpClient2 = new HttpClient();
            const string subscriptionKey = "11353e12efd34147a54b3914bb575f44";
            httpClient2.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            const string endpoint2 = "https://southcentralus.api.cognitive.microsoft.com/vision/v2.0/ocr?language=unk&detectOrientation=true";

            HttpResponseMessage response2;

            byte[] byteData = GetImageAsByteArray(_foto.Path);

            using (UserDialogs.Instance.Loading("Loading..."))
            {

                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");

                    response2 = await httpClient2.PostAsync(endpoint2, content);
                }

                if (!response2.IsSuccessStatusCode)
                {
                    UserDialogs.Instance.Toast("A ocurrido un error en ocr");
                    return;
                }

                text.Text = "";
                List<Region> regions = new List<Region>();
                List<Line> lines = new List<Line>();
                List<Word> words = new List<Word>();
                var json2 = await response2.Content.ReadAsStringAsync();
                string str="";
                string PrimerApellido = "";
                string SegundoApellido = "";
                string Nombre = "";
                string Dni = "";

                var textObject = JsonConvert.DeserializeObject<TextObject>(json2);

                regions = textObject.regions.ToList();

                foreach (var r in regions)
                {
                    lines.AddRange(r.lines.ToList());
                }

                foreach (var l in lines)
                {
                    words.AddRange(l.words.ToList());
                }

                foreach (var w in words)
                {
                    text.Text = $"{text.Text} {w.text}";
                    //str = $"{text.Text} {w.text}";
                }

                //             < Label x: Name = "PrimerApellido" />

                //< Label x: Name = "SegundoApellido" />

                //  < Label x: Name = "Nombre" />

                //    < Label x: Name = "Dni" />
                
                 PrimerApellido = getBetween(text.Text, "APELLIDO", "SEGUNDO");
                
                SegundoApellido = getBetween(str, "SEGUNDO APELLIDO", "NOMBRE");
                Nombre = getBetween(str, "NOMBRE", "SEXO");
                Dni = getBetween(str, "NUM", "");

            }
        }

        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }

        public static string getBetween(string strSource, string strStart, string strEnd)
        {
            int Start, End;
            if (strSource.Contains(strStart) && (strSource.Contains(strEnd) || strEnd == ""))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                if (strEnd != "")
                {
                    End = strSource.IndexOf(strEnd, Start);
                    return strSource.Substring(Start, End - Start);
                }
                else
                {
                    End = Start + 10;
                    return strSource.Substring(Start, End - Start);
                }
            }
            else
            {
                return "";
            }
        }




    }
}
