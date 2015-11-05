using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DmtMax.Infrastructure.Context;
using DmtMax.Infrastructure.Model;
using DmtMax.Infrastructure.Repo;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace CarParasBll
{
    public class CarParasOperation
    {

        public void Test()
        {
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(1000);
            }
        }

        public async Task GetAutoHomeCarSeriesData()
        {

            using (var dbContex = new DmtMaxContext())
            {
                IRepository<CarSeries> CarSeriesRepo = new Repo<CarSeries>(dbContex);
                var results = await CarSeriesRepo.SearchFor(i => true).OrderBy(i => i.ExtranetId).
                              ToListAsync();
                foreach (var i in results)
                {
                    var item = i;
                    var sid = item.ExtranetId;
                    using (var client = new HttpClient())
                    {
                        var httpMessage =
                            await client.GetAsync(string.Format("http://car.autohome.com.cn/price/series-{0}.html",
                                sid));
                        var htmlstr = await httpMessage.Content.ReadAsStringAsync();

                        Regex reg =
                            new Regex(
                                "href=\"(/config/series/[0-9-]+.html#pvareaid=\\d{0,12})\"");
                        var result = reg.Match(htmlstr).Groups;

                        string CarTypeUrl = string.Empty;
                        if (result.Count > 1)
                            CarTypeUrl = result[1].Value;

                        var carTypeDetailhttpMessage =
                            await client.GetAsync(string.Format("http://car.autohome.com.cn{0}",
                                CarTypeUrl));

                        var carTypeDetailhtmlstr = await carTypeDetailhttpMessage.Content.ReadAsStringAsync(); //车系html

                        var keylinkreg = new Regex("(?<=var keyLink =).*?}}(?=;)");
                        var configReg = new Regex("(?<=var config =).*?}}(?=;)");
                        var colorReg = new Regex("(?<=var color =).*?}}(?=;)");
                        var innerColorReg = new Regex("(?<=var innerColor=).*?}}(?=;)");
                        var bagReg = new Regex("(?<=var bag =).*?}(?=;)");
                        var optionsReg = new Regex("(?<=var option =).*?}(?=;)");

                        var keylink = keylinkreg.Match(carTypeDetailhtmlstr).Groups[0].Value;
                        var config = configReg.Match(carTypeDetailhtmlstr).Groups[0].Value;
                        var color = colorReg.Match(carTypeDetailhtmlstr).Groups[0].Value;
                        var innerColor = innerColorReg.Match(carTypeDetailhtmlstr).Groups[0].Value;
                        var bag = bagReg.Match(carTypeDetailhtmlstr).Groups[0].Value;
                        var options = optionsReg.Match(carTypeDetailhtmlstr).Groups[0].Value;
                        B04 b04 = new B04()
                        {
                            B03Id = sid,
                            B03KeyId = 0,
                            bag = bag,
                            color = color,
                            config = config,
                            JsonStr = "",
                            KeyLink = keylink,
                            Url = CarTypeUrl,
                            innerColor = innerColor,
                            option = options
                        };

                        CreateFile(AppDomain.CurrentDomain.BaseDirectory + "AutoData/" + sid + ".txt",
                            JsonConvert.SerializeObject(b04));

                    }
                }
            }
            //    return true;
        }


        /// <summary>
        /// 导入基本参数数据
        /// </summary>
        /// <returns></returns>
        public async Task ImportBasePartas()
        {
            using (var dbContex = new DmtMaxContext())
            {
                IRepository<CarSeries> CarSeriesRepo = new Repo<CarSeries>(dbContex);
                var results = await CarSeriesRepo.SearchFor(i => true).OrderBy(i => i.ExtranetId).
                    ToListAsync();

                foreach (var i in results)
                {
                    var item = i;
                    var fileStr = AppDomain.CurrentDomain.BaseDirectory + "AutoData/" + item.ExtranetId + ".txt";
                    if (File.Exists(fileStr))
                    {
                        string valueStr = ReadFile(fileStr);
                        var config = JsonConvert.DeserializeObject<B04>(valueStr);
                        await UpdateCarNormarlParas(config.config);
                        //  await UpdateCarSpecialParas(config.option);
                        //  await UpdateCarColors(config.color, config.innerColor);
                    }

                }
            }

        }
        /// <summary>
        /// 导入特殊参数数据
        /// </summary>
        /// <returns></returns>
        public async Task ImportOptionPartas()
        {
            using (var dbContex = new DmtMaxContext())
            {
                IRepository<CarSeries> CarSeriesRepo = new Repo<CarSeries>(dbContex);
                var results = await CarSeriesRepo.SearchFor(i => true).OrderBy(i => i.ExtranetId).
                    ToListAsync();
                foreach (var i in results)
                {
                    try
                    {

                        var item = i;
                        var fileStr = AppDomain.CurrentDomain.BaseDirectory + "AutoData/" + item.ExtranetId + ".txt";
                        if (File.Exists(fileStr))
                        {
                            string valueStr = ReadFile(fileStr);
                            var config = JsonConvert.DeserializeObject<B04>(valueStr);
                            await UpdateCarSpecialParas(config.option);
                        }
                    }
                    catch (Exception ex)
                    {

                    }


                }
            }

        }
        /// <summary>
        /// 颜色参数
        /// </summary>
        /// <returns></returns>
        public async Task ImportColorPartas()
        {

            using (var dbContex = new DmtMaxContext())
            {

                IRepository<CarSeries> CarSeriesRepo = new Repo<CarSeries>(dbContex);
                var results = await CarSeriesRepo.SearchFor(i => true).OrderBy(i => i.ExtranetId).
                    ToListAsync();
                foreach (var i in results)
                {
                    var item = i;
                    var fileStr = AppDomain.CurrentDomain.BaseDirectory + "AutoData/" + item.ExtranetId + ".txt";
                    if (File.Exists(fileStr))
                    {
                        string valueStr = ReadFile(fileStr);
                        var config = JsonConvert.DeserializeObject<B04>(valueStr);
                        //   await UpdateCarNormarlParas(config.config);
                        //  await UpdateCarSpecialParas(config.option);
                        await UpdateCarColors(config.color, config.innerColor);
                    }

                }

            }
        }

        /// <summary>
        /// 基本参数
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public async Task<bool> UpdateCarNormarlParas(string str)
        {
            using (var dbContex = new DmtMaxContext())
            {
                IRepository<CarBasicParas> CarBasicParasRepo = new Repo<CarBasicParas>(dbContex);
                IRepository<CarBodyParas> CarBodyParasRepo = new Repo<CarBodyParas>(dbContex);
                IRepository<CarEngineParas> CarEngineParasRepo = new Repo<CarEngineParas>(dbContex);
                IRepository<CarSpeedBox> CarSpeedBoxRepo = new Repo<CarSpeedBox>(dbContex);
                IRepository<CarBottomParas> CarBottomParasRepo = new Repo<CarBottomParas>(dbContex);
                IRepository<CarWheelParas> CarWheelParasRepo = new Repo<CarWheelParas>(dbContex);
                // string str = File.ReadAllText(@"D:\\参数.txt", Encoding.UTF8);
                var listBasic = new List<CarBasicParas>();
                var listBody = new List<CarBodyParas>();
                var listEngin = new List<CarEngineParas>();
                var listBox = new List<CarSpeedBox>();
                var listBottom = new List<CarBottomParas>();
                var listWheel = new List<CarWheelParas>();
                try
                {
                    #region 1.转换成 类

                    var parasInfo = JsonConvert.DeserializeObject<JObject>(str)["result"];
                    int carSerise;
                    int.TryParse(((JValue)parasInfo["seriesid"]).ToString(CultureInfo.InvariantCulture), out carSerise);
                    var listparas = (JArray)parasInfo["paramtypeitems"];
                    var listCarModelParas = new List<CarParaModels>();
                    foreach (var basePara in listparas)
                    {
                        var carParaModels = new CarParaModels();
                        var bigParaName = ((JValue)basePara["name"]).ToString(CultureInfo.InvariantCulture);
                        carParaModels.BigParaName = bigParaName;
                        var childPara = (JArray)basePara["paramitems"];
                        var dicModelParas = new Dictionary<string, List<CarProperty>>();
                        carParaModels.DicModelParas = dicModelParas;
                        foreach (var child in childPara)
                        {
                            var childParaName = ((JValue)child["name"]).ToString(CultureInfo.InvariantCulture);
                            var valueitems = (JArray)child["valueitems"];
                            foreach (var item in valueitems)
                            {
                                var extandId = ((JValue)item["specid"]).ToString(CultureInfo.InvariantCulture);
                                var value = ((JValue)item["value"]).ToString(CultureInfo.InvariantCulture);
                                if (!dicModelParas.ContainsKey(extandId))
                                    dicModelParas.Add(extandId, new List<CarProperty>());
                                dicModelParas[extandId].Add(new CarProperty
                                {
                                    PropertyName = childParaName,
                                    PropertyValue = value
                                });
                            }
                        }
                        listCarModelParas.Add(carParaModels);
                    }

                    #endregion

                    #region 2 数据转换

                    foreach (var model in listCarModelParas)
                    {
                        if (model.BigParaName.Equals("基本参数"))
                        {
                            foreach (var paras in model.DicModelParas)
                            {
                                var basepara = new CarBasicParas();
                                int id;
                                int.TryParse(paras.Key, out id);
                                basepara.ProductExtranetId = id;
                                ExchangeModelValue(ref basepara, paras.Value);
                                basepara.SeriesId = carSerise;
                                listBasic.Add(basepara);
                            }
                        }
                        else if (model.BigParaName.Equals("车身"))
                        {
                            foreach (var paras in model.DicModelParas)
                            {
                                var basepara = new CarBodyParas();
                                int id;
                                int.TryParse(paras.Key, out id);
                                basepara.ProductExtranetId = id;
                                ExchangeModelValue(ref basepara, paras.Value);
                                basepara.SeriesId = carSerise;
                                listBody.Add(basepara);
                            }
                        }

                        else if (model.BigParaName.Equals("发动机"))
                        {
                            foreach (var paras in model.DicModelParas)
                            {
                                var basepara = new CarEngineParas();
                                int id;
                                int.TryParse(paras.Key, out id);
                                basepara.ProductExtranetId = id;
                                ExchangeModelValue(ref basepara, paras.Value);
                                basepara.SeriesId = carSerise;
                                listEngin.Add(basepara);
                            }
                        }
                        else if (model.BigParaName.Equals("变速箱"))
                        {
                            foreach (var paras in model.DicModelParas)
                            {
                                var basepara = new CarSpeedBox();
                                int id;
                                int.TryParse(paras.Key, out id);
                                basepara.ProductExtranetId = id;
                                ExchangeModelValue(ref basepara, paras.Value);
                                basepara.SeriesId = carSerise;
                                listBox.Add(basepara);
                            }
                        }
                        else if (model.BigParaName.Equals("底盘转向"))
                        {
                            foreach (var paras in model.DicModelParas)
                            {
                                var basepara = new CarBottomParas();
                                int id;
                                int.TryParse(paras.Key, out id);
                                basepara.ProductExtranetId = id;
                                ExchangeModelValue(ref basepara, paras.Value);
                                basepara.SeriesId = carSerise;
                                listBottom.Add(basepara);
                            }
                        }
                        else if (model.BigParaName.Equals("车轮制动"))
                        {
                            foreach (var paras in model.DicModelParas)
                            {
                                var basepara = new CarWheelParas();
                                int id;
                                int.TryParse(paras.Key, out id);
                                basepara.ProductExtranetId = id;
                                ExchangeModelValue(ref basepara, paras.Value);
                                basepara.SeriesId = carSerise;
                                listWheel.Add(basepara);
                            }
                        }
                    }

                    #endregion
                }
                catch
                {
                    return false;
                }

                #region 3数据保存

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    //1基本
                    var listbaseIds = (from c in listBasic select c.ProductExtranetId).ToList();
                    var listexist =
                        await CarBasicParasRepo.SearchFor(i => listbaseIds.Contains(i.ProductExtranetId)).ToListAsync();
                    foreach (var exist in listexist)
                    {
                        var exist1 = exist;
                        var newBase =
                            (from p in listBasic where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                .FirstOrDefault();
                        if (newBase != null)
                        {
                            ChangeValues(ref exist1, newBase);

                            listBasic.Remove(newBase);
                        }
                    }

                    //2 车身
                    var listBodyIds = (from c in listBody select c.ProductExtranetId).ToList();
                    var listBodyexist =
                        await CarBodyParasRepo.SearchFor(i => listBodyIds.Contains(i.ProductExtranetId)).ToListAsync();
                    foreach (var exist in listBodyexist)
                    {
                        var exist1 = exist;
                        var newBase =
                            (from p in listBody where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                .FirstOrDefault();
                        if (newBase != null)
                        {
                            ChangeValues(ref exist1, newBase);
                            listBody.Remove(newBase);
                        }
                    }


                    //3 发动机
                    var listEngineIds = (from c in listEngin select c.ProductExtranetId).ToList();
                    var listEngineexist =
                        await
                            CarEngineParasRepo.SearchFor(i => listEngineIds.Contains(i.ProductExtranetId)).ToListAsync();
                    foreach (var exist in listEngineexist)
                    {
                        var exist1 = exist;
                        var newBase =
                            (from p in listEngin where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                .FirstOrDefault();
                        if (newBase != null)
                        {
                            ChangeValues(ref exist1, newBase);
                            listEngin.Remove(newBase);
                        }
                    }


                    //变速箱
                    var listBoxIds = (from c in listBox select c.ProductExtranetId).ToList();
                    var listCoxexist =
                        await CarSpeedBoxRepo.SearchFor(i => listBoxIds.Contains(i.ProductExtranetId)).ToListAsync();
                    foreach (var exist in listCoxexist)
                    {
                        var exist1 = exist;
                        var newBase =
                            (from p in listBox where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                .FirstOrDefault();
                        if (newBase != null)
                        {
                            ChangeValues(ref exist1, newBase);
                            listBox.Remove(newBase);
                        }
                    }

                    //底盘转向
                    var listBottomIds = (from c in listBottom select c.ProductExtranetId).ToList();
                    var listBottomexist =
                        await
                            CarBottomParasRepo.SearchFor(i => listBottomIds.Contains(i.ProductExtranetId)).ToListAsync();
                    foreach (var exist in listBottomexist)
                    {
                        var exist1 = exist;
                        var newBase =
                            (from p in listBottom where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                .FirstOrDefault();
                        if (newBase != null)
                        {
                            ChangeValues(ref exist1, newBase);
                            listBottom.Remove(newBase);
                        }
                    }


                    //车轮制动

                    var listWheelIds = (from c in listWheel select c.ProductExtranetId).ToList();
                    var listWheelexist =
                        await CarWheelParasRepo.SearchFor(i => listWheelIds.Contains(i.ProductExtranetId)).ToListAsync();
                    foreach (var exist in listWheelexist)
                    {
                        var exist1 = exist;
                        var newBase =
                            (from p in listWheel where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                .FirstOrDefault();
                        if (newBase != null)
                        {
                            ChangeValues(ref exist1, newBase);
                            listWheel.Remove(newBase);
                        }
                    }
                    await CarBasicParasRepo.EditRangeAsync(listexist);
                    await CarBasicParasRepo.InsertRangeAsync(listBasic);
                    await CarBodyParasRepo.EditRangeAsync(listBodyexist);
                    await CarBodyParasRepo.InsertRangeAsync(listBody);
                    await CarEngineParasRepo.EditRangeAsync(listEngineexist);
                    await CarEngineParasRepo.InsertRangeAsync(listEngin);
                    await CarSpeedBoxRepo.EditRangeAsync(listCoxexist);
                    await CarSpeedBoxRepo.InsertRangeAsync(listBox);
                    await CarBottomParasRepo.EditRangeAsync(listBottomexist);
                    await CarBottomParasRepo.InsertRangeAsync(listBottom);
                    await CarWheelParasRepo.EditRangeAsync(listWheelexist);
                    await CarWheelParasRepo.InsertRangeAsync(listWheel);


                    scope.Complete();
                }
            }

                #endregion

            return true;
        }

        public async Task<bool> UpdateCarSpecialParas(string str)
        {
            try
            {
                using (var dbContex = new DmtMaxContext())
                {
                    IRepository<CarSafeEquipParas> CarSafeEquipParasRepo = new Repo<CarSafeEquipParas>(dbContex);
                    IRepository<CarOperationParas> CarOperationParasRepo = new Repo<CarOperationParas>(dbContex);
                    IRepository<CarOutParas> CarOutParasRepo = new Repo<CarOutParas>(dbContex);
                    IRepository<CarInnerParas> CarInnerParasRepo = new Repo<CarInnerParas>(dbContex);
                    IRepository<CarChairParas> CarChairParasRepo = new Repo<CarChairParas>(dbContex);
                    IRepository<CarMediaParas> CarMediaParasRepo = new Repo<CarMediaParas>(dbContex);
                    IRepository<CarLightParas> CarLightParasRepo = new Repo<CarLightParas>(dbContex);
                    IRepository<CarGlassMirrorParas> CarGlassMirrorParasRepo = new Repo<CarGlassMirrorParas>(dbContex);
                    IRepository<CarAirConditionerParas> CarAirConditionerParasRepo =
                        new Repo<CarAirConditionerParas>(dbContex);
                    IRepository<CarHighTechParas> CarHighTechParasRepo = new Repo<CarHighTechParas>(dbContex);
                    var listCarModelParas = new List<CarSpecialparaModels>();
                    //   string str = File.ReadAllText(@"D:\\特殊参数.txt", Encoding.UTF8);
                    var listEquip = new List<CarSafeEquipParas>();
                    var listOpera = new List<CarOperationParas>();
                    var listOut = new List<CarOutParas>();
                    var listInner = new List<CarInnerParas>();
                    var listChair = new List<CarChairParas>();
                    var listMedia = new List<CarMediaParas>();
                    var listLight = new List<CarLightParas>();
                    var listMirror = new List<CarGlassMirrorParas>();
                    var listAirCon = new List<CarAirConditionerParas>();
                    var listTech = new List<CarHighTechParas>();

                    try
                    {
                        #region  1转换成 类

                        var parasInfo = JsonConvert.DeserializeObject<JObject>(str)["result"];
                        int carSerise;
                        int.TryParse(((JValue)parasInfo["seriesid"]).ToString(CultureInfo.InvariantCulture),
                            out carSerise);
                        var listparas = (JArray)parasInfo["configtypeitems"];
                        foreach (var basePara in listparas)
                        {
                            var carParaModels = new CarSpecialparaModels();
                            var bigParaName = ((JValue)basePara["name"]).ToString(CultureInfo.InvariantCulture);
                            carParaModels.BigParaName = bigParaName;
                            var childPara = (JArray)basePara["configitems"];
                            var dicModelParas = new Dictionary<string, List<CarSpecialProperty>>();
                            carParaModels.DicModelParas = dicModelParas;
                            foreach (var child in childPara)
                            {
                                var childParaName = ((JValue)child["name"]).ToString(CultureInfo.InvariantCulture);
                                var valueitems = (JArray)child["valueitems"];
                                foreach (var item in valueitems)
                                {
                                    var extandId = ((JValue)item["specid"]).ToString(CultureInfo.InvariantCulture);
                                    var value = ((JValue)item["value"]).ToString(CultureInfo.InvariantCulture);
                                    if (!dicModelParas.ContainsKey(extandId))
                                        dicModelParas.Add(extandId, new List<CarSpecialProperty>());
                                    var newPropertys = new SpecialProperty();
                                    try
                                    {
                                        newPropertys.Value =
                                            ((JValue)item["value"]).ToString(CultureInfo.InvariantCulture);
                                        newPropertys.Price =
                                            ((JValue)item["price"]).ToString(CultureInfo.InvariantCulture);
                                    }
                                    catch
                                    {
                                        try
                                        {
                                            var listPrices = JsonConvert.SerializeObject(item["price"]);
                                            newPropertys.Price = listPrices;
                                        }
                                        catch
                                        {
                                            newPropertys.Price = "null";
                                        }
                                    }
                                    dicModelParas[extandId].Add(new CarSpecialProperty
                                    {
                                        PropertyName = childParaName,
                                        PropertyValues = newPropertys
                                    });
                                }
                            }
                            listCarModelParas.Add(carParaModels);
                        }

                        #endregion

                        #region 2生成数据

                        foreach (var model in listCarModelParas)
                        {
                            if (model.BigParaName.Equals("安全装备"))
                            {
                                foreach (var paras in model.DicModelParas)
                                {
                                    var basepara = new CarSafeEquipParas();
                                    int id;
                                    int.TryParse(paras.Key, out id);
                                    basepara.ProductExtranetId = id;
                                    ExchangeSpecModelValue(ref basepara, paras.Value);
                                    basepara.SeriesId = carSerise;
                                    listEquip.Add(basepara);
                                }
                            }
                            else if (model.BigParaName.Equals("操控配置"))
                            {
                                foreach (var paras in model.DicModelParas)
                                {
                                    var basepara = new CarOperationParas();
                                    int id;
                                    int.TryParse(paras.Key, out id);
                                    basepara.ProductExtranetId = id;
                                    ExchangeSpecModelValue(ref basepara, paras.Value);
                                    basepara.SeriesId = carSerise;
                                    listOpera.Add(basepara);
                                }
                            }

                            else if (model.BigParaName.Equals("外部配置"))
                            {
                                foreach (var paras in model.DicModelParas)
                                {
                                    var basepara = new CarOutParas();
                                    int id;
                                    int.TryParse(paras.Key, out id);
                                    basepara.ProductExtranetId = id;
                                    ExchangeSpecModelValue(ref basepara, paras.Value);
                                    basepara.SeriesId = carSerise;
                                    listOut.Add(basepara);
                                }
                            }
                            else if (model.BigParaName.Equals("内部配置"))
                            {
                                foreach (var paras in model.DicModelParas)
                                {
                                    var basepara = new CarInnerParas();
                                    int id;
                                    int.TryParse(paras.Key, out id);
                                    basepara.ProductExtranetId = id;
                                    ExchangeSpecModelValue(ref basepara, paras.Value);
                                    basepara.SeriesId = carSerise;
                                    listInner.Add(basepara);
                                }
                            }
                            else if (model.BigParaName.Equals("座椅配置"))
                            {
                                foreach (var paras in model.DicModelParas)
                                {
                                    var basepara = new CarChairParas();
                                    int id;
                                    int.TryParse(paras.Key, out id);
                                    basepara.ProductExtranetId = id;
                                    ExchangeSpecModelValue(ref basepara, paras.Value);
                                    basepara.SeriesId = carSerise;
                                    listChair.Add(basepara);
                                }
                            }
                            else if (model.BigParaName.Equals("多媒体配置"))
                            {
                                foreach (var paras in model.DicModelParas)
                                {
                                    var basepara = new CarMediaParas();
                                    int id;
                                    int.TryParse(paras.Key, out id);
                                    basepara.ProductExtranetId = id;
                                    ExchangeSpecModelValue(ref basepara, paras.Value);
                                    basepara.SeriesId = carSerise;
                                    listMedia.Add(basepara);
                                }
                            }
                            else if (model.BigParaName.Equals("灯光配置"))
                            {
                                foreach (var paras in model.DicModelParas)
                                {
                                    var basepara = new CarLightParas();
                                    int id;
                                    int.TryParse(paras.Key, out id);
                                    basepara.ProductExtranetId = id;
                                    ExchangeSpecModelValue(ref basepara, paras.Value);
                                    basepara.SeriesId = carSerise;
                                    listLight.Add(basepara);
                                }
                            }
                            else if (model.BigParaName.Equals("玻璃/后视镜"))
                            {
                                foreach (var paras in model.DicModelParas)
                                {
                                    var basepara = new CarGlassMirrorParas();
                                    int id;
                                    int.TryParse(paras.Key, out id);
                                    basepara.ProductExtranetId = id;
                                    ExchangeSpecModelValue(ref basepara, paras.Value);
                                    basepara.SeriesId = carSerise;
                                    listMirror.Add(basepara);
                                }
                            }
                            else if (model.BigParaName.Equals("空调/冰箱"))
                            {
                                foreach (var paras in model.DicModelParas)
                                {
                                    var basepara = new CarAirConditionerParas();
                                    int id;
                                    int.TryParse(paras.Key, out id);
                                    basepara.ProductExtranetId = id;
                                    ExchangeSpecModelValue(ref basepara, paras.Value);
                                    basepara.SeriesId = carSerise;
                                    listAirCon.Add(basepara);
                                }
                            }
                            else if (model.BigParaName.Equals("高科技配置"))
                            {
                                foreach (var paras in model.DicModelParas)
                                {
                                    var basepara = new CarHighTechParas();
                                    int id;
                                    int.TryParse(paras.Key, out id);
                                    basepara.ProductExtranetId = id;
                                    ExchangeSpecModelValue(ref basepara, paras.Value);
                                    basepara.SeriesId = carSerise;
                                    listTech.Add(basepara);
                                }
                            }



                        }

                        #endregion
                    }
                    catch
                    {
                        return false;
                    }


                    #region 保存数据

                    using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                    {
                        #region 处理数据 不存在 新增 存在 则修改

                        //1安全装备
                        var listequipIds = (from c in listEquip select c.ProductExtranetId).ToList();
                        var listequipexist =
                            await
                                CarSafeEquipParasRepo.SearchFor(i => listequipIds.Contains(i.ProductExtranetId))
                                    .ToListAsync();
                        foreach (var exist in listequipexist)
                        {
                            var exist1 = exist;
                            var newBase =
                                (from p in listEquip where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                    .FirstOrDefault();
                            if (newBase != null)
                            {
                                ChangeValues(ref exist1, newBase);
                                listEquip.Remove(newBase);
                            }
                        }

                        //2操控配置
                        var listoperaIds = (from c in listOpera select c.ProductExtranetId).ToList();
                        var listoperapexist =
                            await
                                CarOperationParasRepo.SearchFor(i => listoperaIds.Contains(i.ProductExtranetId))
                                    .ToListAsync();
                        foreach (var exist in listoperapexist)
                        {
                            var exist1 = exist;
                            var newBase =
                                (from p in listOpera where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                    .FirstOrDefault();
                            if (newBase != null)
                            {
                                ChangeValues(ref exist1, newBase);
                                listOpera.Remove(newBase);
                            }
                        }


                        //3外部配置
                        var listoutIds = (from c in listOut select c.ProductExtranetId).ToList();
                        var listoutexist =
                            await CarOutParasRepo.SearchFor(i => listoutIds.Contains(i.ProductExtranetId)).ToListAsync();
                        foreach (var exist in listoutexist)
                        {
                            var exist1 = exist;
                            var newBase =
                                (from p in listOut where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                    .FirstOrDefault();
                            if (newBase != null)
                            {
                                ChangeValues(ref exist1, newBase);
                                listOut.Remove(newBase);
                            }
                        }

                        //4内部部配置
                        var listinnerIds = (from c in listInner select c.ProductExtranetId).ToList();
                        var listinnerexist =
                            await
                                CarInnerParasRepo.SearchFor(i => listinnerIds.Contains(i.ProductExtranetId))
                                    .ToListAsync();
                        foreach (var exist in listinnerexist)
                        {
                            var exist1 = exist;
                            var newBase =
                                (from p in listInner where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                    .FirstOrDefault();
                            if (newBase != null)
                            {
                                ChangeValues(ref exist1, newBase);
                                listInner.Remove(newBase);
                            }
                        }

                        //5座椅配置
                        var listchairIds = (from c in listChair select c.ProductExtranetId).ToList();
                        var listchairexist =
                            await
                                CarChairParasRepo.SearchFor(i => listchairIds.Contains(i.ProductExtranetId))
                                    .ToListAsync();
                        foreach (var exist in listchairexist)
                        {
                            var exist1 = exist;
                            var newBase =
                                (from p in listChair where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                    .FirstOrDefault();
                            if (newBase != null)
                            {
                                ChangeValues(ref exist1, newBase);
                                listChair.Remove(newBase);
                            }
                        }

                        //6多媒体配置
                        var listmediaIds = (from c in listMedia select c.ProductExtranetId).ToList();
                        var listmediaexist =
                            await
                                CarMediaParasRepo.SearchFor(i => listmediaIds.Contains(i.ProductExtranetId))
                                    .ToListAsync();
                        foreach (var exist in listmediaexist)
                        {
                            var exist1 = exist;
                            var newBase =
                                (from p in listMedia where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                    .FirstOrDefault();
                            if (newBase != null)
                            {
                                ChangeValues(ref exist1, newBase);
                                listMedia.Remove(newBase);
                            }
                        }

                        //7灯光配置
                        var listlightIds = (from c in listLight select c.ProductExtranetId).ToList();
                        var listlightexist =
                            await
                                CarLightParasRepo.SearchFor(i => listlightIds.Contains(i.ProductExtranetId))
                                    .ToListAsync();
                        foreach (var exist in listlightexist)
                        {
                            var exist1 = exist;
                            var newBase =
                                (from p in listLight where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                    .FirstOrDefault();
                            if (newBase != null)
                            {
                                ChangeValues(ref exist1, newBase);
                                listLight.Remove(newBase);
                            }
                        }

                        //8玻璃/后视镜
                        var listmirrorIds = (from c in listMirror select c.ProductExtranetId).ToList();
                        var listmirrorexist =
                            await
                                CarGlassMirrorParasRepo.SearchFor(i => listmirrorIds.Contains(i.ProductExtranetId))
                                    .ToListAsync();
                        foreach (var exist in listmirrorexist)
                        {
                            var exist1 = exist;
                            var newBase =
                                (from p in listMirror where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                    .FirstOrDefault();
                            if (newBase != null)
                            {
                                ChangeValues(ref exist1, newBase);
                                listMirror.Remove(newBase);
                            }
                        }

                        //9空调/冰箱
                        var listairIds = (from c in listAirCon select c.ProductExtranetId).ToList();
                        var listairexist =
                            await
                                CarAirConditionerParasRepo.SearchFor(i => listairIds.Contains(i.ProductExtranetId))
                                    .ToListAsync();
                        foreach (var exist in listairexist)
                        {
                            var exist1 = exist;
                            var newBase =
                                (from p in listAirCon where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                    .FirstOrDefault();
                            if (newBase != null)
                            {
                                ChangeValues(ref exist1, newBase);
                                listAirCon.Remove(newBase);
                            }
                        }

                        //10高科技配置
                        var listtecIds = (from c in listTech select c.ProductExtranetId).ToList();
                        var listtecexist =
                            await
                                CarHighTechParasRepo.SearchFor(i => listtecIds.Contains(i.ProductExtranetId))
                                    .ToListAsync();
                        foreach (var exist in listtecexist)
                        {
                            var exist1 = exist;
                            var newBase =
                                (from p in listTech where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                    .FirstOrDefault();
                            if (newBase != null)
                            {
                                ChangeValues(ref exist1, newBase);
                                listTech.Remove(newBase);
                            }
                        }

                        #endregion

                        #region 处理数据  添加到数据库

                        await CarHighTechParasRepo.EditRangeAsync(listtecexist);
                        await CarHighTechParasRepo.InsertRangeAsync(listTech);
                        await CarAirConditionerParasRepo.EditRangeAsync(listairexist);
                        await CarAirConditionerParasRepo.InsertRangeAsync(listAirCon);
                        await CarGlassMirrorParasRepo.EditRangeAsync(listmirrorexist);
                        await CarGlassMirrorParasRepo.InsertRangeAsync(listMirror);
                        await CarLightParasRepo.EditRangeAsync(listlightexist);
                        await CarLightParasRepo.InsertRangeAsync(listLight);
                        await CarMediaParasRepo.EditRangeAsync(listmediaexist);
                        await CarMediaParasRepo.InsertRangeAsync(listMedia);
                        await CarChairParasRepo.EditRangeAsync(listchairexist);
                        await CarChairParasRepo.InsertRangeAsync(listChair);
                        await CarInnerParasRepo.EditRangeAsync(listinnerexist);
                        await CarInnerParasRepo.InsertRangeAsync(listInner);
                        await CarOutParasRepo.EditRangeAsync(listoutexist);
                        await CarOutParasRepo.InsertRangeAsync(listOut);
                        await CarOperationParasRepo.EditRangeAsync(listoperapexist);
                        await CarOperationParasRepo.InsertRangeAsync(listOpera);
                        await CarSafeEquipParasRepo.EditRangeAsync(listequipexist);
                        await CarSafeEquipParasRepo.InsertRangeAsync(listEquip);

                        #endregion

                        scope.Complete();
                    }
                }
            }
            catch (Exception ex)
            {

            }

                    #endregion
            return true;


        }

        public async Task<bool> UpdateCarColors(string outCo, string inCo)
        {
            using (var dbContex = new DmtMaxContext())
            {
                IRepository<CarColorsParas> CarColorsParasRepo = new Repo<CarColorsParas>(dbContex);
                List<CarColorsParas> listColor = new List<CarColorsParas>();
                string str1 = outCo;
                string str2 = inCo;
                try
                {
                    #region 取值


                    //out 
                    if (JsonConvert.DeserializeObject<JObject>(str1) != null &&
                        JsonConvert.DeserializeObject<JObject>(str1)["result"] != null)
                    {
                        var outparasInfo = JsonConvert.DeserializeObject<JObject>(str1)["result"];
                        var outlistparas = (JArray)outparasInfo["specitems"];
                        foreach (var paras in outlistparas)
                        {
                            var color = new CarColorsParas();
                            color.SeriesId = 0;
                            var productId = int.Parse(((JValue)paras["specid"]).ToString(CultureInfo.InvariantCulture));
                            color.ProductExtranetId = productId;
                            var colorvalues = (JArray)paras["coloritems"];
                            var listOutColor = colorvalues.Select(eachcolor => new OutColor()
                            {
                                Name = ((JValue)eachcolor["name"]).ToString(CultureInfo.InvariantCulture),
                                Value = ((JValue)eachcolor["value"]).ToString(CultureInfo.InvariantCulture)
                            }).ToList();
                            color.ListOutColor = listOutColor;
                            listColor.Add(color);
                        }
                        //
                    }

                    if (JsonConvert.DeserializeObject<JObject>(str2) != null &&
                        JsonConvert.DeserializeObject<JObject>(str2)["result"] != null)
                    {
                        var inparasInfo = JsonConvert.DeserializeObject<JObject>(str2)["result"];
                        var inlistparas = (JArray)inparasInfo["specitems"];
                        foreach (var paras in inlistparas)
                        {
                            var color = new CarColorsParas();
                            var productId = int.Parse(((JValue)paras["specid"]).ToString(CultureInfo.InvariantCulture));
                            var exist =
                                (from c in listColor where c.ProductExtranetId == productId select c).FirstOrDefault();

                            color.ProductExtranetId = productId;
                            var colorvalues = (JArray)paras["coloritems"];
                            var listInColor = new List<InColor>();
                            foreach (var eachcolor in colorvalues)
                            {
                                var c = new InColor();
                                c.Name = ((JValue)eachcolor["name"]).ToString(CultureInfo.InvariantCulture);
                                var vls = ((JArray)eachcolor["values"]);
                                var listss =
                                    vls.Select(v => ((JValue)v).ToString(CultureInfo.InvariantCulture)).ToList();
                                c.ListValues = listss;
                                listInColor.Add(c);
                            }
                            if (exist != null)
                                exist.ListInColor = listInColor;
                            else
                            {
                                color.ListInColor = listInColor;
                                listColor.Add(color);
                            }
                        }
                    }

                    #endregion

                    #region 保存

                    var listecolorIds = (from c in listColor select c.ProductExtranetId).ToList();
                    var listcolorexist =
                        await
                            CarColorsParasRepo.SearchFor(i => listecolorIds.Contains(i.ProductExtranetId)).ToListAsync();
                    foreach (var exist in listcolorexist)
                    {
                        var exist1 = exist;
                        var newBase =
                            (from p in listColor where p.ProductExtranetId == exist1.ProductExtranetId select p)
                                .FirstOrDefault();
                        if (newBase != null)
                        {
                            exist1.InColor = newBase.InColor;
                            exist1.OutColor = newBase.OutColor;
                            listColor.Remove(newBase);
                        }
                    }
                    await CarColorsParasRepo.EditRangeAsync(listcolorexist);
                    await CarColorsParasRepo.InsertRangeAsync(listColor);

                    #endregion
                }
                catch
                {

                }
            }
            return true;
        }





     
        public async Task<bool> ImportAutoImgs(int takeCount)
        {
            if (takeCount > 60)
                takeCount = 60;
            if (takeCount <= 0)
                takeCount = 10;
            using (var dbContex = new DmtMaxContext())
            {
                IRepository<CarSeries> CarSeriesRepo = new Repo<CarSeries>(dbContex);
                IRepository<CarSeriesImages> CarSeriesImagesRepo = new Repo<CarSeriesImages>(dbContex);
                var series = await CarSeriesRepo.GetAll().ToListAsync();
                foreach (var item in series)
                {
                    var i = item;

                    try
                    {
                        if (await CarSeriesImagesRepo.CountAysnc(p => p.SeriesId == i.ExtranetId) > 0)
                            continue;
                        using (var client = new HttpClient())
                        {
                            var round = 0;
                            var url = string.Format("http://car.autohome.com.cn/pic/series/{0}-1.html",
                                i.ExtranetId);
                            restart:
                            if (round >= 2)
                                continue;
                            round++;
                            var httpMessage =
                                await client.GetAsync(url);
                            var html = await httpMessage.Content.ReadAsStringAsync();
                            var htmlDoc = new HtmlDocument();
                            htmlDoc.LoadHtml(html);
                            var nodes =
                                htmlDoc.DocumentNode.SelectNodes(
                                    "//*[starts-with(@class,'uibox-con')]/ul/li/a/img");


                            if (nodes == null || nodes.Count == 0) //如果没有车系图片尝试抓取停售车型
                            {
                                url = string.Format("http://car.autohome.com.cn/pic/series-t/{0}-1.html",
                                    i.ExtranetId);
                                goto restart;
                            }

                            var imgs = nodes.Take(takeCount).ToList();

                            var result = new List<string>();
                            var httpClient = new HttpClient();
                            var multipartFormDataContent = new MultipartFormDataContent();
                            foreach (var img in imgs)
                            {
                                var request =
                                    (HttpWebRequest)
                                        WebRequest.Create(img.Attributes["src"].Value.Replace("/t_", "/u_"));
                                request.Method = "GET";
                                var response = request.GetResponse();
                                var streamConent = new StreamContent(response.GetResponseStream());
                                multipartFormDataContent.Add(streamConent);

                            }
                            var responseMessage =
                                httpClient.PostAsync(
                                    "http://oss.meitc.com/api/Upload/dmtmax/auto/series_" + i.ExtranetId,
                                    multipartFormDataContent).Result;
                            var r = await responseMessage.Content.ReadAsStringAsync();
                            result = JsonConvert.DeserializeObject<List<string>>(r);
                            await CarSeriesImagesRepo.InsertAsync(new CarSeriesImages
                            {
                                ListImages = result,
                                SeriesId = i.ExtranetId
                            });

                        }
                    }
                    catch (Exception ex)
                    {
                        CreateFile(AppDomain.CurrentDomain.BaseDirectory + "/log.txt", i.ExtranetId.ToString() + "\r\n",
                            true);
                    }
                }
            }
            return true;

        }



        #region private Method



        /// <summary>
        /// 写入文件(新建文件或者追加内容)
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="content">文件内容</param>
        /// <param name="append">是否为追加文件，为false时创建或覆盖原有文件</param>
        public static void CreateFile(string fileName, string content, bool append = false)
        {
            FileStream fs = new FileStream(fileName, (append) ? FileMode.Append : FileMode.Create);
            //获得字节数组
            byte[] data = System.Text.Encoding.Default.GetBytes(content);
            //开始写入
            fs.Write(data, 0, data.Length);
            //清空缓冲区、关闭流
            fs.Flush();
            fs.Close();
        }
        /// <summary>
        /// 写入文件(新建文件或者追加内容)
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="content">文件内容</param>
        /// <param name="append">是否为追加文件，为false时创建或覆盖原有文件</param>
        public static void CreateFile(string fileName, string content)
        {
            FileStream fs = new FileStream(fileName, FileMode.Create);
            //获得字节数组
            byte[] data = System.Text.Encoding.Default.GetBytes(content);
            //开始写入
            fs.Write(data, 0, data.Length);
            //清空缓冲区、关闭流
            fs.Flush();
            fs.Close();
        }
        private void ChangeValues<T1, T2>(ref T1 oldInfo, T2 newInfo)
        {
            try
            {
                PropertyInfo[] properties = oldInfo.GetType().GetProperties();
                foreach (var property in properties)
                {
                    PropertyInfo p = GetPropertyFromObj(newInfo, property.Name);
                    if (p != null && p.CanWrite && p.Name.ToString().ToUpper() != "ID")
                    {
                        property.SetValue(oldInfo, p.GetValue(newInfo));
                    }
                }
            }
            catch
            {

            }
        }
        private static PropertyInfo GetPropertyFromObj(object obj, string property)
        {
            if (obj == null)
                return null;
            try
            {
                PropertyInfo p = obj.GetType().GetProperty(property);
                return p;
            }
            catch (Exception)
            {
                return null;
            }

        }
        private void ExchangeModelValue<T>(ref T basepara, List<CarProperty> list)
        {

            Type t = basepara.GetType();
            PropertyInfo[] fields = t.GetProperties();//获取指定对象的所有公共属性
            foreach (var field in fields)
            {
                try
                {
                    var displayName = field.GetCustomAttribute<DisplayNameAttribute>().DisplayName;
                    var values =
                        (from c in list where c.PropertyName == displayName select c.PropertyValue).FirstOrDefault();
                    if (values == null)
                        continue;
                    var value = values;
                    if (field.PropertyType == typeof(Decimal?))
                        value = decimal.Parse(value.ToString());
                    if (field.PropertyType == typeof(int?))
                        value = int.Parse(value.ToString());
                    field.SetValue(basepara, value);
                }
                catch
                { continue; }
            }



        }


        /// <summary>  
        /// 读文件  
        /// </summary>  
        /// <param name="path">文件路径</param>  
        /// <returns></returns>  
        public static string ReadFile(string Path)
        {
            try
            {
                StreamReader sr = new StreamReader(Path, System.Text.Encoding.Default);
                string content = sr.ReadToEnd();
                sr.Close();
                sr.Dispose();
                return content;
            }
            catch
            {
                return "";
            }
        }

        private void ExchangeSpecModelValue<T>(ref T basepara, List<CarSpecialProperty> list)
        {
            Type t = basepara.GetType();
            PropertyInfo[] fields = t.GetProperties();//获取指定对象的所有公共属性
            foreach (var field in fields)
            {
                try
                {
                    var displayName = field.GetCustomAttribute<DisplayNameAttribute>().DisplayName;
                    var values =
                        (from c in list where c.PropertyName == displayName select c.PropertyValue).FirstOrDefault();
                    if (values == null)
                        continue;
                    var value = values;
                    field.SetValue(basepara, value);
                }
                catch
                { continue; }
            }



        }

        #endregion

    }
}
