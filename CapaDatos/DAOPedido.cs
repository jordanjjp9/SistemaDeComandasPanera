using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Reflection; // SetPropIfExists/GetProp
using CapaEntidad;

namespace CapaDatos
{
    public class DAOPedido
    {
        private readonly string _cs;
        public DAOPedido() { _cs = Conexion.Cadena; }

        // ================= Helpers =================

        private static string To8(string cod)
        {
            string s = (cod ?? "").Trim();
            if (s.Length == 0) return "00000000";
            if (IsDigits(s)) return s.PadLeft(8, '0');

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++)
                if (char.IsDigit(s[i])) sb.Append(s[i]);
            return sb.Length > 0 ? sb.ToString().PadLeft(8, '0') : "00000000";
        }

        private static string To10(string cod)
        {
            string s = (cod ?? "").Trim();
            if (s.Length == 0) return "0000000000";
            if (IsDigits(s)) return s.PadLeft(10, '0');

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++)
                if (char.IsDigit(s[i])) sb.Append(s[i]);
            return sb.Length > 0 ? sb.ToString().PadLeft(10, '0') : "0000000000";
        }

        private static bool IsDigits(string s)
        {
            for (int i = 0; i < s.Length; i++)
                if (!char.IsDigit(s[i])) return false;
            return s.Length > 0;
        }

        private static decimal Round2(decimal v) { return Math.Round(v, 2, MidpointRounding.AwayFromZero); }
        private static decimal Round4(decimal v) { return Math.Round(v, 4, MidpointRounding.AwayFromZero); }
        private static decimal Round10(decimal v) { return Math.Round(v, 10, MidpointRounding.AwayFromZero); }
        private static string Nz(string s) { return s == null ? "" : s.Trim(); }

        private static string GetStrPropOrEmpty(object obj, string propName)
        {
            if (obj == null) return "";
            var p = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (p == null) return "";
            try
            {
                var v = p.GetValue(obj, null);
                return v == null ? "" : Convert.ToString(v).Trim();
            }
            catch { return ""; }
        }

        private static void SetPropIfExists(object target, string propName, object value)
        {
            if (target == null) return;
            var p = target.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (p == null || !p.CanWrite) return;
            try
            {
                if (value == null)
                {
                    p.SetValue(target, null, null);
                }
                else
                {
                    var converted = Convert.ChangeType(value, p.PropertyType, CultureInfo.InvariantCulture);
                    p.SetValue(target, converted, null);
                }
            }
            catch { }
        }

        private static string NormalizeCombForDb(string comb)
        {
            var s = (comb ?? "").Trim();
            if (s.Length == 0) return "";
            return To10(s);
        }

        private static bool GetSwtIgvFromProducto(SqlConnection cn, SqlTransaction tx, string cod10)
        {
            using (var cmd = new SqlCommand("SELECT SWT_IGV FROM dbo.M_PRODUC WHERE CDG_PROD = @p", cn, tx))
            {
                cmd.Parameters.Add("@p", SqlDbType.Char, 10).Value = To10(cod10);
                object o = cmd.ExecuteScalar();
                if (o == null || o == DBNull.Value) return false;
                var s = Convert.ToString(o).Trim();
                return s == "1"
                    || s.Equals("X", StringComparison.OrdinalIgnoreCase)
                    || s.Equals("S", StringComparison.OrdinalIgnoreCase)
                    || s.Equals("SI", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string ObtenerSiguienteNumPed(SqlConnection cn, SqlTransaction tx)
        {
            using (var cmd = new SqlCommand(
                "SELECT ISNULL(MAX(CONVERT(INT, NUM_PED)),0) FROM dbo.M_PEDIDO WHERE ISNUMERIC(NUM_PED)=1;", cn, tx))
            {
                object o = cmd.ExecuteScalar();
                int last = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
                int next = last + 1;
                return next.ToString("00000000", CultureInfo.InvariantCulture);
            }
        }

        // ================= Insertar Pedido NUEVO =================

        public string InsertarPedido(
            ceMPedido cab,
            Func<string, string> resolverImpresora,
            Func<string, Tuple<decimal?, bool?>> resolverTrib
        )
        {
            if (cab == null) throw new ArgumentNullException("cab");
            if (cab.Detalles == null || cab.Detalles.Count == 0)
                throw new InvalidOperationException("El pedido no contiene detalles.");

            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        string numPed = ObtenerSiguienteNumPed(cn, tx);
                        cab.NUM_PED = numPed;

                        InsertarCabecera(cn, tx, cab);
                        InsertarDetalles(cn, tx, cab, resolverImpresora, resolverTrib);

                        tx.Commit();
                        return numPed;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        private static void InsertarCabecera(SqlConnection cn, SqlTransaction tx, ceMPedido cab)
        {
            const string CDG_CPAG = "000";
            const string CDG_MON_DEF = "001";
            const string SWT_PTV_FIJO = "S";
            const string ORI_AREA = "001";
            const string CDG_PRIO = "000";
            const string CDG_LOC_DEF = "000";

            const decimal POR_TIGV_FIJO = 10.00m;
            const decimal IMP_TDCT_FIJO = 0.00m;
            const decimal POR_TDCT_FIJO = 0.00m;
            const decimal IMP_TISC_FIJO = 0.00m;

            const int SWT_DIST_FIJO = 0;
            const decimal VAL_DPVT = 0.00m;
            const decimal POR_DPVT = 0.00m;
            const decimal POR_IVA = 0.00m;
            const decimal POR_ICA = 0.00m;
            const decimal POR_FTE = 0.00m;
            const decimal VAL_CARG = 0.00m;
            const decimal POR_CARG = 0.00m;

            decimal impStot = Round2(cab.IMP_BASE);
            decimal impTigv = Round2(impStot * (POR_TIGV_FIJO / 100m));
            decimal impTtot = Round2(impStot + impTigv);

            string sql = @"
            INSERT INTO dbo.M_PEDIDO(
                NUM_PED, CDG_VEND, CDG_CPAG, CDG_MON, FEC_PED,
                NUM_OCOM, IMP_STOT, IMP_TIGV, IMP_TDCT, IMP_TTOT, POR_TDCT, POR_TIGV, 
                OBS_PED, SWT_PED, RUC_CLI, SWT_COT, SWT_PTV, ORI_AREA, CDG_AREA, NUM_COT, 
                CDG_USR, CDG_PRIO, IMP_AJU, CDG_LOC, SWT_DIST, REF_PED, SWT_PROD,
                NUM_NSAL, FEC_ENT, NUM_MESA, NUM_PERS, HRA_PED, PND_APR, FEC_APR, USR_APR, HRA_APR,
                VAL_DPVT, POR_DPVT, DCT_APR, CTA_IVA, CTA_ICA, CTA_FTE, POR_IVA, POR_ICA, POR_FTE,
                VAL_RET, VAL_IVA, VAL_ICA, VAL_FTE,
                FEC_ING, FEC_SAL, HRA_ING, HRA_SAL,
                TDC_CLI, DOI_CLI, CDG_NAC, CNT_PED, VAL_CARG, POR_CARG,
                TIP_PTV, CDG_CAJA, CDG_AMB, IMP_TISC
            )
            VALUES(
                @NUM_PED, @CDG_VEND, @CDG_CPAG, @CDG_MON, @FEC_PED,
                @NUM_OCOM, @IMP_STOT, @IMP_TIGV, @IMP_TDCT, @IMP_TTOT, @POR_TDCT, @POR_TIGV,
                @OBS_PED, @SWT_PED, @RUC_CLI, @SWT_COT, @SWT_PTV, @ORI_AREA, @CDG_AREA, @NUM_COT,
                @CDG_USR, @CDG_PRIO, @IMP_AJU, @CDG_LOC, @SWT_DIST, @REF_PED, @SWT_PROD,
                @NUM_NSAL, @FEC_ENT, @NUM_MESA, @NUM_PERS, @HRA_PED, @PND_APR, @FEC_APR, @USR_APR, @HRA_APR,
                @VAL_DPVT, @POR_DPVT, @DCT_APR, @CTA_IVA, @CTA_ICA, @CTA_FTE, @POR_IVA, @POR_ICA, @POR_FTE,
                @VAL_RET, @VAL_IVA, @VAL_ICA, @VAL_FTE,
                @FEC_ING, @FEC_SAL, @HRA_ING, @HRA_SAL,
                @TDC_CLI, @DOI_CLI, @CDG_NAC, @CNT_PED, @VAL_CARG, @POR_CARG,
                @TIP_PTV, @CDG_CAJA, @CDG_AMB, @IMP_TISC
            );";

            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                cmd.Parameters.Add("@NUM_PED", SqlDbType.Char, 8).Value = To8(cab.NUM_PED);
                cmd.Parameters.Add("@CDG_VEND", SqlDbType.Char, 3).Value = (cab.CDG_VEND ?? "").PadLeft(3, '0');
                cmd.Parameters.Add("@CDG_CPAG", SqlDbType.Char, 3).Value = CDG_CPAG;
                cmd.Parameters.Add("@CDG_MON", SqlDbType.Char, 3).Value = CDG_MON_DEF;
                cmd.Parameters.Add("@FEC_PED", SqlDbType.SmallDateTime).Value = cab.FEC_PED;

                cmd.Parameters.Add("@NUM_OCOM", SqlDbType.Char, 60).Value = "";
                cmd.Parameters.Add("@IMP_STOT", SqlDbType.Decimal).Value = impStot;
                cmd.Parameters.Add("@IMP_TIGV", SqlDbType.Decimal).Value = impTigv;
                cmd.Parameters.Add("@IMP_TDCT", SqlDbType.Decimal).Value = 0.00m;
                cmd.Parameters.Add("@IMP_TTOT", SqlDbType.Decimal).Value = impTtot;
                cmd.Parameters.Add("@POR_TDCT", SqlDbType.Decimal).Value = POR_TDCT_FIJO;
                cmd.Parameters.Add("@POR_TIGV", SqlDbType.Decimal).Value = POR_TIGV_FIJO;

                cmd.Parameters.Add("@OBS_PED", SqlDbType.Text).Value = Nz(cab.OBS_PED);
                cmd.Parameters.Add("@SWT_PED", SqlDbType.Char, 1).Value = "";
                cmd.Parameters.Add("@RUC_CLI", SqlDbType.Char, 8).Value = "00000000";
                cmd.Parameters.Add("@SWT_COT", SqlDbType.Decimal).Value = 0;
                cmd.Parameters.Add("@SWT_PTV", SqlDbType.Char, 1).Value = SWT_PTV_FIJO;
                cmd.Parameters.Add("@ORI_AREA", SqlDbType.Char, 3).Value = ORI_AREA;
                cmd.Parameters.Add("@CDG_AREA", SqlDbType.Char, 3).Value = "";
                cmd.Parameters.Add("@NUM_COT", SqlDbType.Char, 8).Value = "";
                cmd.Parameters.Add("@CDG_USR", SqlDbType.Char, 10).Value = Nz(cab.CDG_USR);
                cmd.Parameters.Add("@CDG_PRIO", SqlDbType.Char, 3).Value = CDG_PRIO;

                cmd.Parameters.Add("@IMP_AJU", SqlDbType.Decimal).Value = 0.00m;
                cmd.Parameters.Add("@CDG_LOC", SqlDbType.Char, 3).Value = CDG_LOC_DEF;
                cmd.Parameters.Add("@SWT_DIST", SqlDbType.Decimal).Value = SWT_DIST_FIJO;
                cmd.Parameters.Add("@REF_PED", SqlDbType.Char, 100).Value = "";
                cmd.Parameters.Add("@SWT_PROD", SqlDbType.Char, 1).Value = "";

                cmd.Parameters.Add("@NUM_NSAL", SqlDbType.Char, 10).Value = "";
                cmd.Parameters.Add("@FEC_ENT", SqlDbType.SmallDateTime).Value = DBNull.Value;
                cmd.Parameters.Add("@NUM_MESA", SqlDbType.Char, 3).Value = Nz(cab.NUM_MESA).PadLeft(3, '0');
                if (cab.NUM_PERS.HasValue)
                    cmd.Parameters.Add("@NUM_PERS", SqlDbType.Decimal).Value = cab.NUM_PERS.Value;
                else
                    cmd.Parameters.Add("@NUM_PERS", SqlDbType.Decimal).Value = DBNull.Value;

                cmd.Parameters.Add("@HRA_PED", SqlDbType.Char, 5).Value = DateTime.Now.ToString("HH:mm");
                cmd.Parameters.Add("@PND_APR", SqlDbType.Char, 1).Value = "";
                cmd.Parameters.Add("@FEC_APR", SqlDbType.DateTime).Value = DBNull.Value;
                cmd.Parameters.Add("@USR_APR", SqlDbType.Char, 10).Value = "";
                cmd.Parameters.Add("@HRA_APR", SqlDbType.Char, 5).Value = "";

                cmd.Parameters.Add("@VAL_DPVT", SqlDbType.Decimal).Value = 0.00m;
                cmd.Parameters.Add("@POR_DPVT", SqlDbType.Decimal).Value = 0.00m;
                cmd.Parameters.Add("@DCT_APR", SqlDbType.Char, 10).Value = "";
                cmd.Parameters.Add("@CTA_IVA", SqlDbType.Char, 10).Value = "";
                cmd.Parameters.Add("@CTA_ICA", SqlDbType.Char, 10).Value = "";
                cmd.Parameters.Add("@CTA_FTE", SqlDbType.Char, 10).Value = "";
                cmd.Parameters.Add("@POR_IVA", SqlDbType.Decimal).Value = POR_IVA;
                cmd.Parameters.Add("@POR_ICA", SqlDbType.Decimal).Value = POR_ICA;
                cmd.Parameters.Add("@POR_FTE", SqlDbType.Decimal).Value = POR_FTE;

                cmd.Parameters.Add("@VAL_RET", SqlDbType.Decimal).Value = 0.00m;
                cmd.Parameters.Add("@VAL_IVA", SqlDbType.Decimal).Value = 0.00m;
                cmd.Parameters.Add("@VAL_ICA", SqlDbType.Decimal).Value = 0.00m;
                cmd.Parameters.Add("@VAL_FTE", SqlDbType.Decimal).Value = 0.00m;

                cmd.Parameters.Add("@FEC_ING", SqlDbType.SmallDateTime).Value = DBNull.Value;
                cmd.Parameters.Add("@FEC_SAL", SqlDbType.SmallDateTime).Value = DBNull.Value;
                cmd.Parameters.Add("@HRA_ING", SqlDbType.Char, 5).Value = "";
                cmd.Parameters.Add("@HRA_SAL", SqlDbType.Char, 5).Value = "";

                cmd.Parameters.Add("@TDC_CLI", SqlDbType.Char, 3).Value = "";
                cmd.Parameters.Add("@DOI_CLI", SqlDbType.Char, 15).Value = "";
                cmd.Parameters.Add("@CDG_NAC", SqlDbType.Char, 3).Value = "";
                cmd.Parameters.Add("@CNT_PED", SqlDbType.Char, 60).Value = "";
                cmd.Parameters.Add("@VAL_CARG", SqlDbType.Decimal).Value = 0.00m;
                cmd.Parameters.Add("@POR_CARG", SqlDbType.Decimal).Value = 0.00m;

                cmd.Parameters.Add("@TIP_PTV", SqlDbType.Char, 1).Value = "2";
                cmd.Parameters.Add("@CDG_CAJA", SqlDbType.Char, 3).Value = Nz(cab.CDG_CAJA);
                cmd.Parameters.Add("@CDG_AMB", SqlDbType.Char, 3).Value = Nz(cab.CDG_AMB).PadLeft(3, '0');
                cmd.Parameters.Add("@IMP_TISC", SqlDbType.Decimal).Value = IMP_TISC_FIJO;

                cmd.Parameters["@IMP_STOT"].Precision = 15; cmd.Parameters["@IMP_STOT"].Scale = 2;
                cmd.Parameters["@IMP_TIGV"].Precision = 15; cmd.Parameters["@IMP_TIGV"].Scale = 2;
                cmd.Parameters["@IMP_TDCT"].Precision = 15; cmd.Parameters["@IMP_TDCT"].Scale = 2;
                cmd.Parameters["@IMP_TTOT"].Precision = 15; cmd.Parameters["@IMP_TTOT"].Scale = 2;
                cmd.Parameters["@POR_TDCT"].Precision = 7; cmd.Parameters["@POR_TDCT"].Scale = 2;
                cmd.Parameters["@POR_TIGV"].Precision = 7; cmd.Parameters["@POR_TIGV"].Scale = 2;
                cmd.Parameters["@NUM_PERS"].Precision = 3; cmd.Parameters["@NUM_PERS"].Scale = 0;

                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertarDetalles(
            SqlConnection cn, SqlTransaction tx, ceMPedido cab,
            Func<string, string> resolverImpresora,
            Func<string, Tuple<decimal?, bool?>> resolverTrib)
        {
            const string CDG_FPRD = "000";
            const decimal CAN_DPRD = 0.0000m;
            const decimal CAN_FPRD_DEF = 0.0000m;
            const decimal DCT_CERO = 0.00m;
            const decimal IGV_PPRD_CERO = 0.00m;

            const decimal FAC_UVTA = 1.0000000000m;
            const string CDG_UVTA_DEF = "001";

            const decimal COM_PPRD = 0.00m;
            const decimal CAN_PROD = 0.0000m;
            const decimal CAN_OTRB = 0.0000m;

            const decimal POR_TISC = 0.00m;
            const decimal PCT_CARG = 0.00m;
            const decimal IMP_CARG = 0.00m;

            const string ORI_PED = "";
            const string TDC_COMA = "";
            const string DOC_COMA = "";
            const decimal POR_IGV_100 = 10.00m;

            // Validación previa de productos
            var cods = new HashSet<string>();
            foreach (var dd in cab.Detalles)
            {
                string c10 = To10(dd.COD10 ?? dd.CDG_PROD.ToString(CultureInfo.InvariantCulture));
                if (!string.IsNullOrWhiteSpace(c10)) cods.Add(c10);
            }
            var faltantes = new List<string>();
            foreach (string c10 in cods)
            {
                using (var cmdChk = new SqlCommand("SELECT COUNT(1) FROM dbo.M_PRODUC WHERE CDG_PROD = @C", cn, tx))
                {
                    cmdChk.Parameters.Add("@C", SqlDbType.Char, 10).Value = c10;
                    int n = Convert.ToInt32(cmdChk.ExecuteScalar());
                    if (n == 0) faltantes.Add(c10);
                }
            }
            if (faltantes.Count > 0)
                throw new InvalidOperationException("CDG_PROD inexistentes en M_PRODUC: " + string.Join(", ", faltantes));

            string sql = @"
            INSERT INTO dbo.D_PEDIDO(
                NUM_PED, CDG_PROD, CDG_FPRD, CAN_PPRD, PRE_PPRD, DCT_PPRD, DCT_FIC, IGV_PPRD, IMP_TPRD,
                CAN_DPRD, CAN_FPRD, OBS_PPRD, CDG_LPRC, PRE_IGV, IMP_IGV,
                CDG_PROM, FAC_UVTA, CDG_UVTA, COM_PPRD, CAN_PROD, CAN_OTRB, CAN_UVTA, PRE_UVTA, VAL_UVTA, TOT_UVTA,
                POR_TISC, swt_igv, com_impo, SAC_PPRD, SWT_CMP, POR_IGV, IMP_IVA, NUM_ITEM, IMP_PROD, SWT_IMPR,
                PCT_CARG, IMP_CARG, ORI_PED, CDG_COMB, TDC_COMA, DOC_COMA
            )
            VALUES(
                @NUM_PED, @CDG_PROD, @CDG_FPRD, @CAN_PPRD, @PRE_PPRD, @DCT_PPRD, @DCT_FIC, @IGV_PPRD, @IMP_TPRD,
                @CAN_DPRD, @CAN_FPRD, @OBS_PPRD, @CDG_LPRC, @PRE_IGV, @IMP_IGV,
                @CDG_PROM, @FAC_UVTA, @CDG_UVTA, @COM_PPRD, @CAN_PROD, @CAN_OTRB, @CAN_UVTA, @PRE_UVTA, @VAL_UVTA, @TOT_UVTA,
                @POR_TISC, @swt_igv, @com_impo, @SAC_PPRD, @SWT_CMP, @POR_IGV, @IMP_IVA, @NUM_ITEM, @IMP_PROD, @SWT_IMPR,
                @PCT_CARG, @IMP_CARG, @ORI_PED, @CDG_COMB, @TDC_COMA, @DOC_COMA
            );";

            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                int item = 0;

                foreach (var d in cab.Detalles)
                {
                    item++;

                    string cod10 = To10(d.COD10 ?? d.CDG_PROD.ToString(CultureInfo.InvariantCulture));
                    decimal can = Round4(d.CAN_PPRD);
                    decimal pre = Round4(d.PRE_PPRD);
                    decimal impt = Round2(d.IMP_TPRD);
                    decimal preI = Round4(d.PRE_IGV);
                    decimal igvI = Round2(d.IMP_IGV);

                    string notas = Nz(d.OBS_PPRD);
                    string lprc = (d.CDG_LPRC <= 0) ? "001" : d.CDG_LPRC.ToString("000");

                    decimal preUvta = preI;
                    decimal valUvta = pre;
                    decimal totUvta = impt;
                    decimal impIva = Math.Max(0m, Math.Round(igvI - impt, 2));

                    string swt_igv_text = "";
                    bool hasSwt = false;
                    if (resolverTrib != null)
                    {
                        var t = resolverTrib(cod10);
                        if (t != null && t.Item2.HasValue)
                        {
                            hasSwt = true;
                            if (t.Item2.Value) swt_igv_text = "X";
                        }
                    }
                    if (!hasSwt)
                    {
                        bool afecto = GetSwtIgvFromProducto(cn, tx, cod10);
                        swt_igv_text = afecto ? "X" : "";
                    }

                    string imp = (resolverImpresora != null) ? Nz(resolverImpresora(cod10)) : "";
                    string swtImpr = (imp.Length == 0) ? "" : "X";

                    string combInput = GetStrPropOrEmpty(d, "CDG_COMB");
                    string combDb = NormalizeCombForDb(combInput);

                    cmd.Parameters.Clear();

                    cmd.Parameters.Add("@NUM_PED", SqlDbType.Char, 8).Value = To8(cab.NUM_PED);
                    cmd.Parameters.Add("@CDG_PROD", SqlDbType.Char, 10).Value = cod10;
                    cmd.Parameters.Add("@CDG_FPRD", SqlDbType.Char, 3).Value = CDG_FPRD;

                    cmd.Parameters.Add("@CAN_PPRD", SqlDbType.Decimal).Value = can;
                    cmd.Parameters.Add("@PRE_PPRD", SqlDbType.Decimal).Value = pre;
                    cmd.Parameters.Add("@DCT_PPRD", SqlDbType.Decimal).Value = DCT_CERO;
                    cmd.Parameters.Add("@DCT_FIC", SqlDbType.Decimal).Value = DCT_CERO;
                    cmd.Parameters.Add("@IGV_PPRD", SqlDbType.Decimal).Value = IGV_PPRD_CERO;
                    cmd.Parameters.Add("@IMP_TPRD", SqlDbType.Decimal).Value = impt;

                    cmd.Parameters.Add("@CAN_DPRD", SqlDbType.Decimal).Value = CAN_DPRD;
                    cmd.Parameters.Add("@CAN_FPRD", SqlDbType.Decimal).Value = CAN_FPRD_DEF;
                    cmd.Parameters.Add("@OBS_PPRD", SqlDbType.Text).Value = notas;
                    cmd.Parameters.Add("@CDG_LPRC", SqlDbType.Char, 3).Value = lprc;
                    cmd.Parameters.Add("@PRE_IGV", SqlDbType.Decimal).Value = preI;
                    cmd.Parameters.Add("@IMP_IGV", SqlDbType.Decimal).Value = igvI;

                    cmd.Parameters.Add("@CDG_PROM", SqlDbType.Char, 10).Value = "";
                    cmd.Parameters.Add("@FAC_UVTA", SqlDbType.Decimal).Value = Round10(FAC_UVTA);
                    cmd.Parameters.Add("@CDG_UVTA", SqlDbType.Char, 3).Value = CDG_UVTA_DEF;
                    cmd.Parameters.Add("@COM_PPRD", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@CAN_PROD", SqlDbType.Decimal).Value = 0.0000m;
                    cmd.Parameters.Add("@CAN_OTRB", SqlDbType.Decimal).Value = 0.0000m;

                    cmd.Parameters.Add("@CAN_UVTA", SqlDbType.Decimal).Value = can;
                    cmd.Parameters.Add("@PRE_UVTA", SqlDbType.Decimal).Value = preUvta;
                    cmd.Parameters.Add("@VAL_UVTA", SqlDbType.Decimal).Value = pre;
                    cmd.Parameters.Add("@TOT_UVTA", SqlDbType.Decimal).Value = impt;

                    cmd.Parameters.Add("@POR_TISC", SqlDbType.Decimal).Value = POR_TISC;
                    cmd.Parameters.Add("@swt_igv", SqlDbType.Char, 1).Value = swt_igv_text;
                    cmd.Parameters.Add("@com_impo", SqlDbType.Decimal).Value = 0.00m;

                    cmd.Parameters.Add("@SAC_PPRD", SqlDbType.Char, 15).Value = "";
                    cmd.Parameters.Add("@SWT_CMP", SqlDbType.Char, 1).Value = "";
                    cmd.Parameters.Add("@POR_IGV", SqlDbType.Decimal).Value = POR_IGV_100;
                    cmd.Parameters.Add("@IMP_IVA", SqlDbType.Decimal).Value = impIva;

                    cmd.Parameters.Add("@NUM_ITEM", SqlDbType.Char, 5).Value = item.ToString("00000", CultureInfo.InvariantCulture);
                    cmd.Parameters.Add("@IMP_PROD", SqlDbType.Char, 3).Value = Nz(imp);
                    cmd.Parameters.Add("@SWT_IMPR", SqlDbType.Char, 1).Value = swtImpr;

                    cmd.Parameters.Add("@PCT_CARG", SqlDbType.Decimal).Value = PCT_CARG;
                    cmd.Parameters.Add("@IMP_CARG", SqlDbType.Decimal).Value = IMP_CARG;
                    cmd.Parameters.Add("@ORI_PED", SqlDbType.Char, 8).Value = ORI_PED;

                    cmd.Parameters.Add("@CDG_COMB", SqlDbType.Char, 10).Value = combDb;
                    cmd.Parameters.Add("@TDC_COMA", SqlDbType.Char, 3).Value = TDC_COMA;
                    cmd.Parameters.Add("@DOC_COMA", SqlDbType.Char, 10).Value = DOC_COMA;

                    cmd.Parameters["@CAN_PPRD"].Precision = 15; cmd.Parameters["@CAN_PPRD"].Scale = 4;
                    cmd.Parameters["@PRE_PPRD"].Precision = 15; cmd.Parameters["@PRE_PPRD"].Scale = 4;
                    cmd.Parameters["@IMP_TPRD"].Precision = 15; cmd.Parameters["@IMP_TPRD"].Scale = 2;
                    cmd.Parameters["@PRE_IGV"].Precision = 15; cmd.Parameters["@PRE_IGV"].Scale = 4;
                    cmd.Parameters["@IMP_IGV"].Precision = 15; cmd.Parameters["@IMP_IGV"].Scale = 2;

                    cmd.Parameters["@FAC_UVTA"].Precision = 15; cmd.Parameters["@FAC_UVTA"].Scale = 10;
                    cmd.Parameters["@CAN_UVTA"].Precision = 15; cmd.Parameters["@CAN_UVTA"].Scale = 4;
                    cmd.Parameters["@PRE_UVTA"].Precision = 15; cmd.Parameters["@PRE_UVTA"].Scale = 4;
                    cmd.Parameters["@VAL_UVTA"].Precision = 15; cmd.Parameters["@VAL_UVTA"].Scale = 4;
                    cmd.Parameters["@TOT_UVTA"].Precision = 15; cmd.Parameters["@TOT_UVTA"].Scale = 2;

                    cmd.Parameters["@POR_TISC"].Precision = 7; cmd.Parameters["@POR_TISC"].Scale = 2;
                    cmd.Parameters["@com_impo"].Precision = 15; cmd.Parameters["@com_impo"].Scale = 2;
                    cmd.Parameters["@POR_IGV"].Precision = 7; cmd.Parameters["@POR_IGV"].Scale = 2;
                    cmd.Parameters["@IMP_IVA"].Precision = 15; cmd.Parameters["@IMP_IVA"].Scale = 2;

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ===================== Anexar a pedido EXISTENTE =====================

        public void AnexarSoloDetalles(
            string numPed8,
            IList<ceDPedido> detallesNuevos,
            Func<string, string> resolverImpresora,
            Func<string, Tuple<decimal?, bool?>> resolverTrib
        )
        {
            if (string.IsNullOrWhiteSpace(numPed8))
                throw new ArgumentException("NUM_PED vacío.", "numPed8");

            if (detallesNuevos == null || detallesNuevos.Count == 0)
                return;

            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        AsegurarExistePedido(cn, tx, numPed8, true);

                        InsertarDetallesEnPedidoExistente(cn, tx, numPed8, detallesNuevos, resolverImpresora, resolverTrib);

                        RecalcularTotalesCabeceraDesdeDetalle(cn, tx, numPed8);

                        using (var cmdF = new SqlCommand(
                            "UPDATE dbo.M_PEDIDO SET FEC_PED = CASE WHEN FEC_PED IS NULL THEN GETDATE() ELSE FEC_PED END WHERE NUM_PED = @P;", cn, tx))
                        {
                            cmdF.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);
                            cmdF.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        private static void AsegurarExistePedido(SqlConnection cn, SqlTransaction tx, string numPed8, bool debeEstarAbierto)
        {
            using (var cmd = new SqlCommand(
                @"SELECT COUNT(1) FROM dbo.M_PEDIDO WHERE NUM_PED=@P;", cn, tx))
            {
                cmd.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);
                int n = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
                if (n == 0) throw new InvalidOperationException("El pedido " + To8(numPed8) + " no existe.");
            }

            if (debeEstarAbierto)
            {
                using (var cmd = new SqlCommand(
                    @"SELECT CASE WHEN (SWT_PED IS NULL OR LTRIM(RTRIM(SWT_PED))='') THEN 1 ELSE 0 END 
                      FROM dbo.M_PEDIDO WHERE NUM_PED=@P;", cn, tx))
                {
                    cmd.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);
                    object o = cmd.ExecuteScalar();
                    bool abierto = (o != null && Convert.ToInt32(o, CultureInfo.InvariantCulture) == 1);
                    if (!abierto) throw new InvalidOperationException("El pedido " + To8(numPed8) + " ya está cerrado/anulado.");
                }
            }
        }

        private static int ObtenerMaxNumItem(SqlConnection cn, SqlTransaction tx, string numPed8)
        {
            using (var cmd = new SqlCommand(
                @"SELECT ISNULL(MAX(CONVERT(INT, NULLIF(NUM_ITEM,''))),0) 
                  FROM dbo.D_PEDIDO WHERE NUM_PED=@P;", cn, tx))
            {
                cmd.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);
                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
        }

        private static int ObtenerMaxCdgComb(SqlConnection cn, SqlTransaction tx, string numPed8)
        {
            using (var cmd = new SqlCommand(
                @"SELECT ISNULL(MAX(CONVERT(INT, NULLIF(CDG_COMB,''))),0) 
                  FROM dbo.D_PEDIDO WHERE NUM_PED=@P;", cn, tx))
            {
                cmd.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);
                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
        }

        private static void InsertarDetallesEnPedidoExistente(
            SqlConnection cn, SqlTransaction tx, string numPed8,
            IList<ceDPedido> detallesNuevos,
            Func<string, string> resolverImpresora,
            Func<string, Tuple<decimal?, bool?>> resolverTrib)
        {
            var cods = new HashSet<string>();
            foreach (var d in detallesNuevos)
            {
                string c10 = To10(d.COD10 ?? d.CDG_PROD.ToString(CultureInfo.InvariantCulture));
                if (!string.IsNullOrWhiteSpace(c10)) cods.Add(c10);
            }
            var faltantes = new List<string>();
            foreach (string c10 in cods)
            {
                using (var cmdChk = new SqlCommand("SELECT COUNT(1) FROM dbo.M_PRODUC WHERE CDG_PROD = @C", cn, tx))
                {
                    cmdChk.Parameters.Add("@C", SqlDbType.Char, 10).Value = c10;
                    int n = Convert.ToInt32(cmdChk.ExecuteScalar());
                    if (n == 0) faltantes.Add(c10);
                }
            }
            if (faltantes.Count > 0)
                throw new InvalidOperationException("CDG_PROD inexistentes en M_PRODUC: " + string.Join(", ", faltantes));

            string sql = @"
            INSERT INTO dbo.D_PEDIDO(
                NUM_PED, CDG_PROD, CDG_FPRD, CAN_PPRD, PRE_PPRD, DCT_PPRD, DCT_FIC, IGV_PPRD, IMP_TPRD,
                CAN_DPRD, CAN_FPRD, OBS_PPRD, CDG_LPRC, PRE_IGV, IMP_IGV,
                CDG_PROM, FAC_UVTA, CDG_UVTA, COM_PPRD, CAN_PROD, CAN_OTRB, CAN_UVTA, PRE_UVTA, VAL_UVTA, TOT_UVTA,
                POR_TISC, swt_igv, com_impo, SAC_PPRD, SWT_CMP, POR_IGV, IMP_IVA, NUM_ITEM, IMP_PROD, SWT_IMPR,
                PCT_CARG, IMP_CARG, ORI_PED, CDG_COMB, TDC_COMA, DOC_COMA
            )
            VALUES(
                @NUM_PED, @CDG_PROD, @CDG_FPRD, @CAN_PPRD, @PRE_PPRD, @DCT_PPRD, @DCT_FIC, @IGV_PPRD, @IMP_TPRD,
                @CAN_DPRD, @CAN_FPRD, @OBS_PPRD, @CDG_LPRC, @PRE_IGV, @IMP_IGV,
                @CDG_PROM, @FAC_UVTA, @CDG_UVTA, @COM_PPRD, @CAN_PROD, @CAN_OTRB, @CAN_UVTA, @PRE_UVTA, @VAL_UVTA, @TOT_UVTA,
                @POR_TISC, @swt_igv, @com_impo, @SAC_PPRD, @SWT_CMP, @POR_IGV, @IMP_IVA, @NUM_ITEM, @IMP_PROD, @SWT_IMPR,
                @PCT_CARG, @IMP_CARG, @ORI_PED, @CDG_COMB, @TDC_COMA, @DOC_COMA
            );";

            int nextItem = ObtenerMaxNumItem(cn, tx, numPed8);

            int maxCombExistente = Math.Max(ObtenerMaxCdgComb(cn, tx, numPed8), 99);
            var mapComb = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                foreach (var d in detallesNuevos)
                {
                    nextItem++;

                    string cod10 = To10(d.COD10 ?? d.CDG_PROD.ToString(CultureInfo.InvariantCulture));
                    decimal can = Round4(d.CAN_PPRD);
                    decimal pre = Round4(d.PRE_PPRD);
                    decimal impt = Round2(d.IMP_TPRD);
                    decimal preI = Round4(d.PRE_IGV);
                    decimal igvI = Round2(d.IMP_IGV);

                    string notas = Nz(d.OBS_PPRD);
                    string lprc = (d.CDG_LPRC <= 0) ? "001" : d.CDG_LPRC.ToString("000");

                    decimal preUvta = preI;
                    decimal valUvta = pre;
                    decimal totUvta = impt;
                    decimal impIva = Math.Max(0m, Math.Round(igvI - impt, 2));

                    string swt_igv_text = "";
                    bool hasSwt = false;
                    if (resolverTrib != null)
                    {
                        var t = resolverTrib(cod10);
                        if (t != null && t.Item2.HasValue)
                        {
                            hasSwt = true;
                            if (t.Item2.Value) swt_igv_text = "X";
                        }
                    }
                    if (!hasSwt)
                    {
                        bool afecto = GetSwtIgvFromProducto(cn, tx, cod10);
                        swt_igv_text = afecto ? "X" : "";
                    }

                    string imp = (resolverImpresora != null) ? Nz(resolverImpresora(cod10)) : "";
                    string swtImpr = (imp.Length == 0) ? "" : "X";

                    // remap de CDG_COMB local -> consecutivo real en DB
                    string combInput = GetStrPropOrEmpty(d, "CDG_COMB"); // "100","101",...
                    string combDb = "";
                    if (!string.IsNullOrWhiteSpace(combInput))
                    {
                        int asignado;
                        if (!mapComb.TryGetValue(combInput, out asignado))
                        {
                            asignado = ++maxCombExistente;
                            mapComb[combInput] = asignado;
                        }
                        combDb = To10(asignado.ToString(CultureInfo.InvariantCulture));
                    }

                    cmd.Parameters.Clear();

                    cmd.Parameters.Add("@NUM_PED", SqlDbType.Char, 8).Value = To8(numPed8);
                    cmd.Parameters.Add("@CDG_PROD", SqlDbType.Char, 10).Value = cod10;
                    cmd.Parameters.Add("@CDG_FPRD", SqlDbType.Char, 3).Value = "000";

                    cmd.Parameters.Add("@CAN_PPRD", SqlDbType.Decimal).Value = can;
                    cmd.Parameters.Add("@PRE_PPRD", SqlDbType.Decimal).Value = pre;
                    cmd.Parameters.Add("@DCT_PPRD", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@DCT_FIC", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@IGV_PPRD", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@IMP_TPRD", SqlDbType.Decimal).Value = totUvta;

                    cmd.Parameters.Add("@CAN_DPRD", SqlDbType.Decimal).Value = 0.0000m;
                    cmd.Parameters.Add("@CAN_FPRD", SqlDbType.Decimal).Value = 0.0000m;
                    cmd.Parameters.Add("@OBS_PPRD", SqlDbType.Text).Value = notas;
                    cmd.Parameters.Add("@CDG_LPRC", SqlDbType.Char, 3).Value = lprc;
                    cmd.Parameters.Add("@PRE_IGV", SqlDbType.Decimal).Value = preI;
                    cmd.Parameters.Add("@IMP_IGV", SqlDbType.Decimal).Value = igvI;

                    cmd.Parameters.Add("@CDG_PROM", SqlDbType.Char, 10).Value = "";
                    cmd.Parameters.Add("@FAC_UVTA", SqlDbType.Decimal).Value = Round10(1.0000000000m);
                    cmd.Parameters.Add("@CDG_UVTA", SqlDbType.Char, 3).Value = "001";
                    cmd.Parameters.Add("@COM_PPRD", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@CAN_PROD", SqlDbType.Decimal).Value = 0.0000m;
                    cmd.Parameters.Add("@CAN_OTRB", SqlDbType.Decimal).Value = 0.0000m;

                    cmd.Parameters.Add("@CAN_UVTA", SqlDbType.Decimal).Value = can;
                    cmd.Parameters.Add("@PRE_UVTA", SqlDbType.Decimal).Value = preUvta;
                    cmd.Parameters.Add("@VAL_UVTA", SqlDbType.Decimal).Value = valUvta;
                    cmd.Parameters.Add("@TOT_UVTA", SqlDbType.Decimal).Value = totUvta;

                    cmd.Parameters.Add("@POR_TISC", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@swt_igv", SqlDbType.Char, 1).Value = swt_igv_text;
                    cmd.Parameters.Add("@com_impo", SqlDbType.Decimal).Value = 0.00m;

                    cmd.Parameters.Add("@SAC_PPRD", SqlDbType.Char, 15).Value = "";
                    cmd.Parameters.Add("@SWT_CMP", SqlDbType.Char, 1).Value = "";
                    cmd.Parameters.Add("@POR_IGV", SqlDbType.Decimal).Value = 10.00m;
                    cmd.Parameters.Add("@IMP_IVA", SqlDbType.Decimal).Value = impIva;

                    cmd.Parameters.Add("@NUM_ITEM", SqlDbType.Char, 5).Value = nextItem.ToString("00000", CultureInfo.InvariantCulture);
                    cmd.Parameters.Add("@IMP_PROD", SqlDbType.Char, 3).Value = Nz(imp);
                    cmd.Parameters.Add("@SWT_IMPR", SqlDbType.Char, 1).Value = swtImpr;

                    cmd.Parameters.Add("@PCT_CARG", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@IMP_CARG", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@ORI_PED", SqlDbType.Char, 8).Value = "";

                    cmd.Parameters.Add("@CDG_COMB", SqlDbType.Char, 10).Value = combDb;
                    cmd.Parameters.Add("@TDC_COMA", SqlDbType.Char, 3).Value = "";
                    cmd.Parameters.Add("@DOC_COMA", SqlDbType.Char, 10).Value = "";

                    cmd.Parameters["@CAN_PPRD"].Precision = 15; cmd.Parameters["@CAN_PPRD"].Scale = 4;
                    cmd.Parameters["@PRE_PPRD"].Precision = 15; cmd.Parameters["@PRE_PPRD"].Scale = 4;
                    cmd.Parameters["@IMP_TPRD"].Precision = 15; cmd.Parameters["@IMP_TPRD"].Scale = 2;
                    cmd.Parameters["@PRE_IGV"].Precision = 15; cmd.Parameters["@PRE_IGV"].Scale = 4;
                    cmd.Parameters["@IMP_IGV"].Precision = 15; cmd.Parameters["@IMP_IGV"].Scale = 2;

                    cmd.Parameters["@FAC_UVTA"].Precision = 15; cmd.Parameters["@FAC_UVTA"].Scale = 10;
                    cmd.Parameters["@CAN_UVTA"].Precision = 15; cmd.Parameters["@CAN_UVTA"].Scale = 4;
                    cmd.Parameters["@PRE_UVTA"].Precision = 15; cmd.Parameters["@PRE_UVTA"].Scale = 4;
                    cmd.Parameters["@VAL_UVTA"].Precision = 15; cmd.Parameters["@VAL_UVTA"].Scale = 4;
                    cmd.Parameters["@TOT_UVTA"].Precision = 15; cmd.Parameters["@TOT_UVTA"].Scale = 2;

                    cmd.Parameters["@POR_TISC"].Precision = 7; cmd.Parameters["@POR_TISC"].Scale = 2;
                    cmd.Parameters["@com_impo"].Precision = 15; cmd.Parameters["@com_impo"].Scale = 2;
                    cmd.Parameters["@POR_IGV"].Precision = 7; cmd.Parameters["@POR_IGV"].Scale = 2;
                    cmd.Parameters["@IMP_IVA"].Precision = 15; cmd.Parameters["@IMP_IVA"].Scale = 2;

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void RecalcularTotalesCabeceraDesdeDetalle(SqlConnection cn, SqlTransaction tx, string numPed8)
        {
            decimal impStot = 0m, impIgv = 0m;

            using (var cmd = new SqlCommand(@"
                SELECT 
                    ISNULL(SUM(IMP_TPRD),0)   AS ST,
                    ISNULL(SUM(IMP_IGV),0)   AS IGV
                FROM dbo.D_PEDIDO
                WHERE NUM_PED = @P;", cn, tx))
            {
                cmd.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);
                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        impStot = rd["ST"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["ST"]);
                        impIgv = rd["IGV"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IGV"]);
                    }
                }
            }

            decimal baseCalc = Round2(impStot);
            decimal igvCalc = Round2(baseCalc * 0.10m);
            decimal totCalc = Round2(baseCalc + igvCalc);

            using (var cmdU = new SqlCommand(@"
                UPDATE dbo.M_PEDIDO
                   SET IMP_STOT = @ST,
                       IMP_TIGV = @IGV,
                       IMP_TTOT = @TTOT
                 WHERE NUM_PED = @P;", cn, tx))
            {
                cmdU.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);

                cmdU.Parameters.Add("@ST", SqlDbType.Decimal).Value = baseCalc;
                cmdU.Parameters.Add("@IGV", SqlDbType.Decimal).Value = igvCalc;
                cmdU.Parameters.Add("@TTOT", SqlDbType.Decimal).Value = totCalc;

                cmdU.Parameters["@ST"].Precision = 15; cmdU.Parameters["@ST"].Scale = 2;
                cmdU.Parameters["@IGV"].Precision = 15; cmdU.Parameters["@IGV"].Scale = 2;
                cmdU.Parameters["@TTOT"].Precision = 15; cmdU.Parameters["@TTOT"].Scale = 2;

                cmdU.ExecuteNonQuery();
            }
        }

        // ================= Consultas =================

        public string ObtenerNumPedAbiertoPorMesa(string cdgMesa)
        {
            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var cmd = new SqlCommand(@"
                SELECT TOP 1 m.NUM_PED
                FROM dbo.M_PEDIDO m
                WHERE 
                    (
                        m.NUM_MESA = @MESA
                        OR RIGHT('00' + m.NUM_MESA, 3) = RIGHT('00' + @MESA, 3)
                        OR RIGHT('00' + @MESA, 3) = RIGHT('00' + m.NUM_MESA, 3)
                    )
                    AND (m.SWT_PED IS NULL OR LTRIM(RTRIM(m.SWT_PED)) = '')
                ORDER BY m.FEC_PED DESC;", cn))
                {
                    cmd.Parameters.Add("@MESA", SqlDbType.VarChar, 3).Value =
                        (cdgMesa ?? "").Trim().PadLeft(3, '0');

                    object o = cmd.ExecuteScalar();
                    return (o == null || o == DBNull.Value) ? null : Convert.ToString(o);
                }
            }
        }

        public bool PedidoSigueAbierto(string numPed8)
        {
            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var cmd = new SqlCommand(@"
                SELECT CASE WHEN (SWT_PED IS NULL OR LTRIM(RTRIM(SWT_PED)) = '') THEN 1 ELSE 0 END
                FROM dbo.M_PEDIDO
                WHERE NUM_PED = @P;", cn))
                {
                    cmd.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);
                    var r = cmd.ExecuteScalar();
                    return r != null && Convert.ToInt32(r) == 1;
                }
            }
        }

        public ceMPedido ObtenerCabeceraPorNum(string numPed8)
        {
            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var cmd = new SqlCommand(@"
                SELECT 
                    NUM_PED, NUM_MESA, SWT_PED, FEC_PED, CDG_VEND, CDG_AMB, NUM_PERS,
                    IMP_STOT, IMP_TIGV, IMP_TTOT
                FROM dbo.M_PEDIDO
                WHERE NUM_PED = @P;", cn))
                {
                    cmd.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);

                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read()) return null;

                        return new ceMPedido
                        {
                            NUM_PED = Convert.ToString(rd["NUM_PED"]),
                            NUM_MESA = Convert.ToString(rd["NUM_MESA"]).Trim(),
                            SWT_PED = Convert.ToString(rd["SWT_PED"]).Trim(),
                            FEC_PED = rd["FEC_PED"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(rd["FEC_PED"]),
                            CDG_VEND = Convert.ToString(rd["CDG_VEND"]).Trim(),
                            CDG_AMB = Convert.ToString(rd["CDG_AMB"]).Trim(),
                            NUM_PERS = rd["NUM_PERS"] == DBNull.Value ? (int?)null : Convert.ToInt32(rd["NUM_PERS"]),
                            IMP_BASE = rd["IMP_STOT"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IMP_STOT"]),
                        };
                    }
                }
            }
        }

        //public List<ceDPedido> ObtenerDetallePorPedido(string numPed8)
        //{
        //    var lista = new List<ceDPedido>();
        //    using (var cn = new SqlConnection(_cs))
        //    {
        //        cn.Open();
        //        using (var cmd = new SqlCommand(@"
        //        SELECT 
        //            d.NUM_PED,
        //            d.CDG_PROD,
        //            d.CAN_PPRD, d.PRE_PPRD, d.IMP_TPRD,
        //            d.PRE_IGV, d.IMP_IGV,
        //            d.OBS_PPRD,
        //            d.CDG_LPRC,
        //            d.NUM_ITEM,
        //            d.IMP_PROD,
        //            d.SWT_IMPR,
        //            d.CDG_COMB,
        //            p.DES_PROD AS DESCRIPCION
        //        FROM dbo.D_PEDIDO d
        //        LEFT JOIN dbo.M_PRODUC p
        //               ON p.CDG_PROD = d.CDG_PROD
        //        WHERE d.NUM_PED = @P
        //        ORDER BY d.NUM_ITEM;", cn))
        //        {
        //            cmd.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);

        //            using (var rd = cmd.ExecuteReader())
        //            {
        //                while (rd.Read())
        //                {
        //                    string cod10 = rd["CDG_PROD"] == DBNull.Value ? "" : Convert.ToString(rd["CDG_PROD"]).Trim();
        //                    int cdgProdInt = 0; int.TryParse(cod10, out cdgProdInt);

        //                    var d = new ceDPedido
        //                    {
        //                        COD10 = cod10,
        //                        CDG_PROD = cdgProdInt,
        //                        CAN_PPRD = rd["CAN_PPRD"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["CAN_PPRD"]),
        //                        PRE_PPRD = rd["PRE_PPRD"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["PRE_PPRD"]),
        //                        IMP_TPRD = rd["IMP_TPRD"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IMP_TPRD"]),
        //                        PRE_IGV = rd["PRE_IGV"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["PRE_IGV"]),
        //                        IMP_IGV = rd["IMP_IGV"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IMP_IGV"]),
        //                        OBS_PPRD = rd["OBS_PPRD"] == DBNull.Value ? "" : Convert.ToString(rd["OBS_PPRD"]).Trim(),
        //                        CDG_LPRC = rd["CDG_LPRC"] == DBNull.Value ? 0 : Convert.ToInt32(rd["CDG_LPRC"]),
        //                        IMP_PROD = rd["IMP_PROD"] == DBNull.Value ? "" : Convert.ToString(rd["IMP_PROD"]).Trim(),
        //                        SWT_IMPR = rd["SWT_IMPR"] == DBNull.Value ? (bool?)null :
        //                                   (Convert.ToString(rd["SWT_IMPR"]).Trim().Equals("X", StringComparison.OrdinalIgnoreCase) ? true : (bool?)false),
        //                        DESCRIPCION = rd["DESCRIPCION"] == DBNull.Value ? "" : Convert.ToString(rd["DESCRIPCION"]).Trim()
        //                    };

        //                    // === NUEVO: Propagar NUM_ITEM ===
        //                    string numItemDb = rd["NUM_ITEM"] == DBNull.Value ? "" : Convert.ToString(rd["NUM_ITEM"]).Trim();
        //                    if (!string.IsNullOrEmpty(numItemDb))
        //                    {
        //                        // Normalizamos a 5 dígitos por consistencia con la DB
        //                        SetPropIfExists(d, "NUM_ITEM", numItemDb.PadLeft(5, '0'));
        //                    }

        //                    // Ya mapeabas CDG_COMB; mantenemos y normalizamos para el usuario
        //                    string combDb = rd["CDG_COMB"] == DBNull.Value ? "" : Convert.ToString(rd["CDG_COMB"]).Trim();
        //                    if (!string.IsNullOrEmpty(combDb))
        //                    {
        //                        string combUser = IsDigits(combDb) ? Convert.ToInt32(combDb).ToString() : combDb;
        //                        SetPropIfExists(d, "CDG_COMB", combUser);
        //                    }

        //                    lista.Add(d);
        //                }
        //            }
        //        }
        //    }
        //    return lista;
        //}
        public List<ceDPedido> ObtenerDetallePorPedido(string numPed8)
        {
            var lista = new List<ceDPedido>();

            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();

                using (var cmd = new SqlCommand(@"
            SELECT
                d.NUM_PED,
                d.CDG_FPRD,             -- ★ clave del detalle (fallback)
                d.NUM_ITEM,             -- ★ identificador exacto de la fila
                d.CDG_COMB,             -- ★ id de grupo (combo/menú) si aplica
                d.CDG_PROD,
                d.CAN_PPRD, d.PRE_PPRD, d.IMP_TPRD,
                d.PRE_IGV,  d.IMP_IGV,
                d.OBS_PPRD,
                d.CDG_LPRC,
                d.IMP_PROD,
                d.SWT_IMPR,
                p.DES_PROD AS DESCRIPCION
            FROM dbo.D_PEDIDO d
            LEFT JOIN dbo.M_PRODUC p
                   ON p.CDG_PROD = d.CDG_PROD
            WHERE d.NUM_PED = @P
            ORDER BY d.NUM_ITEM;", cn))
                {
                    cmd.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);

                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            // CDG_PROD puede venir numérico; lo expresamos también como COD10 (10 dígitos)
                            string cdgProdStr = rd["CDG_PROD"] == DBNull.Value ? "" : Convert.ToString(rd["CDG_PROD"]).Trim();
                            int cdgProdInt = 0;
                            int.TryParse(cdgProdStr, out cdgProdInt);
                            string cod10 = cdgProdInt > 0 ? cdgProdInt.ToString().PadLeft(10, '0') : cdgProdStr.PadLeft(10, '0');

                            var d = new ceDPedido
                            {
                                // claves / datos base
                                CDG_PROD = cdgProdInt,
                                COD10 = cod10,

                                // montos
                                CAN_PPRD = rd["CAN_PPRD"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["CAN_PPRD"]),
                                PRE_PPRD = rd["PRE_PPRD"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["PRE_PPRD"]),
                                IMP_TPRD = rd["IMP_TPRD"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IMP_TPRD"]),
                                PRE_IGV = rd["PRE_IGV"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["PRE_IGV"]),
                                IMP_IGV = rd["IMP_IGV"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IMP_IGV"]),

                                // otros campos
                                OBS_PPRD = rd["OBS_PPRD"] == DBNull.Value ? "" : Convert.ToString(rd["OBS_PPRD"]).Trim(),
                                CDG_LPRC = rd["CDG_LPRC"] == DBNull.Value ? 0 : Convert.ToInt32(rd["CDG_LPRC"]),
                                IMP_PROD = rd["IMP_PROD"] == DBNull.Value ? "" : Convert.ToString(rd["IMP_PROD"]).Trim(),
                                SWT_IMPR = rd["SWT_IMPR"] == DBNull.Value ? (bool?)null
                                            : (Convert.ToString(rd["SWT_IMPR"]).Trim().Equals("X", StringComparison.OrdinalIgnoreCase) ? true : (bool?)false),
                                DESCRIPCION = rd["DESCRIPCION"] == DBNull.Value ? "" : Convert.ToString(rd["DESCRIPCION"]).Trim()
                            };

                            // ★ CDG_FPRD (clave del detalle)
                            if (rd["CDG_FPRD"] != DBNull.Value)
                                d.CDG_FPRD = Convert.ToInt32(rd["CDG_FPRD"]);

                            // ★ NUM_ITEM (left-pad a 5 si aplica)
                            string numItemDb = rd["NUM_ITEM"] == DBNull.Value ? "" : Convert.ToString(rd["NUM_ITEM"]).Trim();
                            if (!string.IsNullOrEmpty(numItemDb))
                            {
                                // si es numérico, normaliza a 5 dígitos; si no, deja el texto original
                                string norm = IsDigits(numItemDb) ? numItemDb.PadLeft(5, '0') : numItemDb;
                                SetPropIfExists(d, "NUM_ITEM", norm);   // o d.NUM_ITEM = norm; si la propiedad existe
                            }

                            // ★ CDG_COMB (normaliza visual si fue numérico con ceros)
                            string combDb = rd["CDG_COMB"] == DBNull.Value ? "" : Convert.ToString(rd["CDG_COMB"]).Trim();
                            if (!string.IsNullOrEmpty(combDb))
                            {
                                string combUser = IsDigits(combDb) ? Convert.ToInt32(combDb).ToString() : combDb;
                                SetPropIfExists(d, "CDG_COMB", combUser); // o d.CDG_COMB = combUser;
                            }

                            lista.Add(d);
                        }
                    }
                }
            }

            return lista;
        }


        // ======= Eliminación por selección (combo o línea suelta) + recalcular =======

        /// <summary>
        /// Elimina detalles de un pedido:
        /// - Si cdgComb tiene valor, elimina todas las filas de ese combo/menú (CDG_COMB).
        /// - Si numItem tiene valor, elimina SOLO esa línea (NUM_ITEM).
        /// Luego recalcula los totales de la cabecera.
        /// </summary>
        public int EliminarDetallesSeleccion(string numPed8, string cdgComb /*puede ser null*/, string numItem /*puede ser null*/)
        {
            if (string.IsNullOrWhiteSpace(numPed8))
                throw new ArgumentException("NUM_PED vacío.", "numPed8");

            int filas = 0;

            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        // borrar por CDG_COMB
                        if (!string.IsNullOrWhiteSpace(cdgComb))
                        {
                            using (var cmd = new SqlCommand(
                                @"DELETE FROM dbo.D_PEDIDO 
                                  WHERE NUM_PED = @n AND CDG_COMB = @c;", cn, tx))
                            {
                                cmd.Parameters.Add("@n", SqlDbType.Char, 8).Value = To8(numPed8);
                                cmd.Parameters.Add("@c", SqlDbType.Char, 10).Value = To10(cdgComb);
                                filas += cmd.ExecuteNonQuery();
                            }
                        }

                        // borrar por NUM_ITEM (línea suelta)
                        if (!string.IsNullOrWhiteSpace(numItem))
                        {
                            using (var cmd = new SqlCommand(
                                @"DELETE FROM dbo.D_PEDIDO 
                                  WHERE NUM_PED = @n AND NUM_ITEM = @i;", cn, tx))
                            {
                                cmd.Parameters.Add("@n", SqlDbType.Char, 8).Value = To8(numPed8);
                                cmd.Parameters.Add("@i", SqlDbType.Char, 5).Value = numItem.Trim().PadLeft(5, '0');
                                filas += cmd.ExecuteNonQuery();
                            }
                        }

                        // Recalcular totales
                        RecalcularTotalesCabeceraDesdeDetalle(cn, tx, numPed8);

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }

            return filas;
        }

        // ========= NUEVO: atajos explícitos para borrar UNA línea o un combo =========
        public int EliminarDetallePorNumItem(string numPed8, string numItem5)
            => EliminarDetallesSeleccion(numPed8, null, numItem5);

        public int EliminarDetallePorCombo(string numPed8, string cdgComb)
            => EliminarDetallesSeleccion(numPed8, cdgComb, null);

        // ========= Compatibilidad: elimina por CDG_FPRD (tu método original) =========
        public int EliminarDetalle(string numPed, int cdgFprd)
        {
            const string SQL = @"DELETE FROM dbo.D_PEDIDO WHERE NUM_PED=@n AND CDG_FPRD=@f;";
            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(SQL, cn))
            {
                cmd.Parameters.AddWithValue("@n", (numPed ?? "").Trim());
                cmd.Parameters.AddWithValue("@f", cdgFprd);
                cn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        // ========= Recalcular Totales (versión rápida a partir de PRE_IGV*CAN_PPRD) =========
        public void RecalcularTotales(string numPed)
        {
            const decimal POR_IGV = 0.18m; // cambia a 0.10m si tu IGV es 10%
            const string SQL = @"
            ;WITH x AS (
                SELECT ISNULL(SUM(PRE_IGV * CAN_PPRD),0) AS TotConIgv
                FROM dbo.D_PEDIDO
                WHERE NUM_PED = @n
            )
            UPDATE dbo.M_PEDIDO
               SET IMP_STOT = ROUND(x.TotConIgv / (1 + @pIgv), 2),
                   IMP_TIGV = ROUND(x.TotConIgv - (x.TotConIgv / (1 + @pIgv)), 2),
                   IMP_TTOT = ROUND(x.TotConIgv, 2)
              FROM x
             WHERE NUM_PED = @n;";

            using (var cn = new SqlConnection(Conexion.Cadena))
            using (var cmd = new SqlCommand(SQL, cn))
            {
                cmd.Parameters.AddWithValue("@n", (numPed ?? "").Trim());
                cmd.Parameters.Add("@pIgv", SqlDbType.Decimal).Value = POR_IGV;
                cmd.Parameters["@pIgv"].Precision = 5; cmd.Parameters["@pIgv"].Scale = 2;

                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
