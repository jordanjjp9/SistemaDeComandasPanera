using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class cePedido
    {
        //M_PED
        public string NUM_PED {  get; set; }
        public string CDG_VEND { get; set; }
        public string CDG_CPAG { get; set; }
        public string CDG_MON { get; set; }
        public DateTime FEC_PED { get; set; }
        public decimal IMP_STOT { get; set; }
        public decimal IMP_TIGV { get; set; }
        public decimal IMP_TDCT { get; set; }
        public decimal IMP_TTOT { get; set; }
        public decimal POR_TDCT { get; set; }
        public decimal POR_TIGV { get; set; }
        public int RUC_CLI { get; set; }
        public int SWT_COT { get; set; }
        public DateTime FEC_ANUL { get; set; }
        public string SWT_PTV { get; set; }
        public string ORI_AREA { get; set; }
        public string CDG_USR { get; set; }
        public string CDG_PRIO { get; set; }
        public decimal IMP_AJU { get; set; }
        public string CDG_LOC { get; set; }
        public string SWT_PROD { get; set; }
        public decimal IMP_TISC { get; set; }
        public string FEC_ENT { get; set; }
        public string NUM_MESA { get; set; }
        public string NUM_PERS { get; set; }
        public DateTime HRA_PERS { get; set; }
        public DateTime FEC_APR { get; set; }
        public decimal VAL_DPVT { get; set; }
        public decimal POR_DPVT { get; set; }
        public decimal POR_IVA { get; set; }
        public decimal POR_ICA { get; set; }
        public decimal POR_FTE { get; set; }
        public decimal VAL_RET { get; set; }
        public decimal VAL_IVA { get; set; }
        public decimal VAL_ICA { get; set; }
        public decimal FEC_ING { get; set; }
        public DateTime FEC_SAL { get; set; }
        public decimal VAL_CARG { get; set; }
        public decimal POR_CARG { get;set; }
        public string TIP_PTV { get; set; }
        public string CDG_AMB { get; set; }

        //D_PED
        public string CDG_PROD { get; set; }
        public string CDG_FPRD { get; set; }
        public decimal CAN_PPRD { get; set; }
        public decimal PRE_PPRD { get; set; }
        public decimal DCT_PPRD { get; set; }
        public decimal DCT_FIC { get; set; }
        public decimal IGV_PPRD { get; set; }
        public decimal IMP_TPRD { get; set; }
        public decimal CAN_DPRD { get; set; }
        public decimal CAN_FPRD { get; set; }
        public string OBS_PPRD { get; set; }
        public string PRE_IGV { get; set; }
        public decimal IMP_IGV { get; set; }
        public decimal FAC_UVTA { get; set; }
        public string CDG_UVTA { get; set; }
        public decimal COM_PPRD { get; set; }
        public decimal CAN_PROD { get; set; }
        public decimal CAN_OTRB { get; set; }
        public decimal CAN_UVTA { get; set; }
        public decimal PRE_UVTA { get; set; }
        public decimal VAL_UVTA { get; set; }
        public decimal TOT_UVTA { get; set; }
        public decimal POR_TISC { get; set; }
        public string swt_igv { get; set; }
        public decimal com_impo { get; set; }
        public decimal POR_IGV { get; set; }
        public decimal IMP_IVA { get; set; }
        public string NUM_ITEM { get; set; }
        public string IMP_PROD { get; set; } // impresora
        public string SWT_IMPR { get; set; } // si cuenta con impresora activa
        public decimal PCT_CARG { get; set; }
        public decimal IMP_CARG { get; set; }
    }
}
