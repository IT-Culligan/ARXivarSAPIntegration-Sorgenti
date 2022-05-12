using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArxivarSAPIntegration
{
    public static class IntegratorHelper
    {
        //    public static List<SAP_Testate_Outbound_Delivery_Arxivar> GetListaOutbound()
        //    {
        //        using (SAP_BI_oldEntities context = new SAP_BI_oldEntities())
        //        {
        //            //var obj = (from t
        //            //          in context.SAP_Testate_Outbound_Delivery_Arxivar
        //            //           where t.DeliveryDocument.StartsWith("8") && (t.Arxivar_Letto == false || t.Arxivar_Letto == null)
        //            //           select t).ToList();

        //            var obj = (from t
        //                      in context.SAP_Testate_Outbound_Delivery_Arxivar
        //                       where t.DeliveryDocument.Equals("80491204")
        //                       select t).ToList();

        //            if (obj != null)
        //            {
        //                return obj;
        //            }
        //        }

        //        return null;
        //    }

        //    public static List<SAP_Open_Item_CE_Fornitori> GetFatturePassive()
        //    {
        //        using (SAP_BI_oldEntities context = new SAP_BI_oldEntities())
        //        {
        //            var obj = (from t
        //                      in context.SAP_Open_Item_CE_Fornitori
        //                      where t.Supplier != ""
        //                       select t).ToList();

        //            if (obj != null)
        //            {
        //                return obj;
        //            }
        //        }

        //        return null;
        //    }

        //    public static void WriteResult(string _documentID, string _errorMSG)
        //    {
        //        using (SAP_BI_oldEntities context = new SAP_BI_oldEntities())
        //        {
        //            SAP_Testate_Outbound_Delivery_Arxivar obj = (from t
        //                      in context.SAP_Testate_Outbound_Delivery_Arxivar
        //                                                         where t.DeliveryDocument == _documentID
        //                                                         select t).First();

        //            if (obj != null)
        //            {
        //                obj.Arxivar_Import_Result = _errorMSG;
        //                context.SaveChanges();
        //            }
        //        }
        //    }

        //    public static void SetRead(string _documentID, bool _read)
        //    {
        //        using (SAP_BI_oldEntities context = new SAP_BI_oldEntities())
        //        {
        //            SAP_Testate_Outbound_Delivery_Arxivar obj = (from t
        //                      in context.SAP_Testate_Outbound_Delivery_Arxivar
        //                                                         where t.DeliveryDocument == _documentID
        //                                                         select t).First();

        //            if (obj != null)
        //            {
        //                obj.Arxivar_Letto = _read;
        //                context.SaveChanges();
        //            }
        //        }
        //    }
    }
}
