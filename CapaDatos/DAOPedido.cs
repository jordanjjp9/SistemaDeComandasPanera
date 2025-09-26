using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Reflection;
using CapaEntidad;

namespace CapaDatos
{
    public class DAOPedido
    {
        private readonly string _cs;
        public DAOPedido() { _cs = Conexion.Cadena; }

        // ======== Tasa IGV (10%) ========
        private const decimal TAZA_IGV_100 = 10.00m;
        private const decimal TAZA_IGV_FRAC = 0.10m;
        private const decimal UNO_MAS_IGV = 1.10m;

        // ======== Helpers ========
        private static string To8(string cod)
        {
            string s = (cod ?? "").Trim();
            if (s.Length == 0) return "00000000";
            if (IsDigits(s)) return s.PadLeft(8, '0');
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++) if (char.IsDigit(s[i])) sb.Append(s[i]);
            return sb.Length > 0 ? sb.ToString().PadLeft(8, '0') : "00000000";
        }
        private static string To10(string cod)
        {
            string s = (cod ?? "").Trim();
            if (s.Length == 0) return "0000000000";
            if (IsDigits(s)) return s.PadLeft(10, '0');
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++) if (char.IsDigit(s[i])) sb.Append(s[i]);
            return sb.Length > 0 ? sb.ToString().PadLeft(10, '0') : "0000000000";
        }
        private static bool IsDigits(string s) { for (int i = 0; i < s.Length; i++) if (!char.IsDigit(s[i])) return false; return s.Length > 0; }
        private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal Round4(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);
        private static decimal Round10(decimal v) => Math.Round(v, 10, MidpointRounding.AwayFromZero);
        private static string Nz(string s) => s == null ? "" : s.Trim();

        private static string GetStrPropOrEmpty(object obj, string prop)
        {
            if (obj == null) return "";
            var p = obj.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            if (p == null) return "";
            try { var v = p.GetValue(obj, null); return v == null ? "" : Convert.ToString(v).Trim(); } catch { return ""; }
        }
        private static void SetPropIfExists(object tgt, string prop, object value)
        {
            if (tgt == null) return;
            var p = tgt.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            if (p == null || !p.CanWrite) return;
            try
            {
                if (value == null) p.SetValue(tgt, null, null);
                else p.SetValue(tgt, Convert.ChangeType(value, p.PropertyType, CultureInfo.InvariantCulture), null);
            }
            catch { }
        }
        private static string NormalizeCombForDb(string comb) => To10((comb ?? "").Trim());

        private static bool GetSwtIgvFromProducto(SqlConnection cn, SqlTransaction tx, string cod10)
        {
            using (var cmd = new SqlCommand("SELECT SWT_IGV FROM dbo.M_PRODUC WHERE CDG_PROD = @p", cn, tx))
            {
                cmd.Parameters.Add("@p", SqlDbType.Char, 10).Value = To10(cod10);
                object o = cmd.ExecuteScalar();
                if (o == null || o == DBNull.Value) return false;
                var s = Convert.ToString(o).Trim();
                return s == "1" || s.Equals("X", StringComparison.OrdinalIgnoreCase) || s.Equals("S", StringComparison.OrdinalIgnoreCase) || s.Equals("SI", StringComparison.OrdinalIgnoreCase);
            }
        }
        private static string ObtenerSiguienteNumPed(SqlConnection cn, SqlTransaction tx)
        {
            using (var cmd = new SqlCommand("SELECT ISNULL(MAX(CONVERT(INT, NUM_PED)),0) FROM dbo.M_PEDIDO WHERE ISNUMERIC(NUM_PED)=1;", cn, tx))
            {
                int last = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                return (last + 1).ToString("00000000", CultureInfo.InvariantCulture);
            }
        }
        // Fallback de impresora: resolver() -> si vacío, M_PRODUC.IMP_PROD
        private static string ObtenerImpresoraParaProducto(SqlConnection cn, SqlTransaction tx, string cod10, Func<string, string> resolver)
        {
            string imp = "";
            if (resolver != null) { try { imp = resolver(cod10) ?? ""; } catch { imp = ""; } }
            if (string.IsNullOrWhiteSpace(imp))
            {
                using (var cmd = new SqlCommand("SELECT LTRIM(RTRIM(ISNULL(IMP_PROD,''))) FROM dbo.M_PRODUC WHERE CDG_PROD = @p;", cn, tx))
                {
                    cmd.Parameters.Add("@p", SqlDbType.Char, 10).Value = To10(cod10);
                    object o = cmd.ExecuteScalar();
                    imp = (o == null || o == DBNull.Value) ? "" : Convert.ToString(o).Trim();
                }
            }
            if (IsDigits(imp)) imp = imp.PadLeft(3, '0');
            return imp;
        }

        // ======== Insertar pedido ========
        public string InsertarPedido(ceMPedido cab, Func<string, string> resolverImpresora, Func<string, Tuple<decimal?, bool?>> resolverTrib)
        {
            if (cab == null) throw new ArgumentNullException(nameof(cab));
            if (cab.Detalles == null || cab.Detalles.Count == 0) throw new InvalidOperationException("El pedido no contiene detalles.");

            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        cab.NUM_PED = ObtenerSiguienteNumPed(cn, tx);
                        InsertarCabecera(cn, tx, cab);
                        InsertarDetalles(cn, tx, cab, resolverImpresora, resolverTrib);
                        RecalcularTotalesCabeceraDesdeDetalle(cn, tx, cab.NUM_PED);
                        tx.Commit();
                        return cab.NUM_PED;
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        //private static void InsertarCabecera(SqlConnection cn, SqlTransaction tx, ceMPedido cab)
        //{
        //    const string CDG_CPAG = "001"; // SIEMPRE '001'
        //    const string CDG_MON_DEF = "001";
        //    const string SWT_PTV_FIJO = "S";
        //    const string ORI_AREA = "001";
        //    const string CDG_PRIO = "000";
        //    const string CDG_LOC_DEF = "000";

        //    const decimal IMP_TDCT_FIJO = 0.00m, POR_TDCT_FIJO = 0.00m, IMP_TISC_FIJO = 0.00m;
        //    const int SWT_DIST_FIJO = 0;
        //    const decimal VAL_DPVT = 0.00m, POR_DPVT = 0.00m, POR_IVA = 0.00m, POR_ICA = 0.00m, POR_FTE = 0.00m, VAL_CARG = 0.00m, POR_CARG = 0.00m;

        //    decimal impStot = 0m, impTigv = 0m, impTtot = 0m; // se recalcula luego

        //    string sql = @"
        //    INSERT INTO dbo.M_PEDIDO(
        //        NUM_PED, CDG_VEND, CDG_CPAG, CDG_MON, FEC_PED,
        //        NUM_OCOM, IMP_STOT, IMP_TIGV, IMP_TDCT, IMP_TTOT, POR_TDCT, POR_TIGV,
        //        OBS_PED, SWT_PED, RUC_CLI, SWT_COT, SWT_PTV, ORI_AREA, CDG_AREA, NUM_COT,
        //        CDG_USR, CDG_PRIO, IMP_AJU, CDG_LOC, SWT_DIST, REF_PED, SWT_PROD,
        //        NUM_NSAL, FEC_ENT, NUM_MESA, NUM_PERS, HRA_PED, PND_APR, FEC_APR, USR_APR, HRA_APR,
        //        VAL_DPVT, POR_DPVT, DCT_APR, CTA_IVA, CTA_ICA, CTA_FTE, POR_IVA, POR_ICA, POR_FTE,
        //        VAL_RET, VAL_IVA, VAL_ICA, VAL_FTE,
        //        FEC_ING, FEC_SAL, HRA_ING, HRA_SAL,
        //        TDC_CLI, DOI_CLI, CDG_NAC, CNT_PED, VAL_CARG, POR_CARG,
        //        TIP_PTV, CDG_CAJA, CDG_AMB, IMP_TISC)
        //    VALUES(
        //        @NUM_PED, @CDG_VEND, @CDG_CPAG, @CDG_MON, @FEC_PED,
        //        @NUM_OCOM, @IMP_STOT, @IMP_TIGV, @IMP_TDCT, @IMP_TTOT, @POR_TDCT, @POR_TIGV,
        //        @OBS_PED, @SWT_PED, @RUC_CLI, @SWT_COT, @SWT_PTV, @ORI_AREA, @CDG_AREA, @NUM_COT,
        //        @CDG_USR, @CDG_PRIO, @IMP_AJU, @CDG_LOC, @SWT_DIST, @REF_PED, @SWT_PROD,
        //        @NUM_NSAL, @FEC_ENT, @NUM_MESA, @NUM_PERS, @HRA_PED, @PND_APR, @FEC_APR, @USR_APR, @HRA_APR,
        //        @VAL_DPVT, @POR_DPVT, @DCT_APR, @CTA_IVA, @CTA_ICA, @CTA_FTE, @POR_IVA, @POR_ICA, @POR_FTE,
        //        @VAL_RET, @VAL_IVA, @VAL_ICA, @VAL_FTE,
        //        @FEC_ING, @FEC_SAL, @HRA_ING, @HRA_SAL,
        //        @TDC_CLI, @DOI_CLI, @CDG_NAC, @CNT_PED, @VAL_CARG, @POR_CARG,
        //        @TIP_PTV, @CDG_CAJA, @CDG_AMB, @IMP_TISC);";

        //    using (var cmd = new SqlCommand(sql, cn, tx))
        //    {
        //        cmd.Parameters.Add("@NUM_PED", SqlDbType.Char, 8).Value = To8(cab.NUM_PED);
        //        cmd.Parameters.Add("@CDG_VEND", SqlDbType.Char, 3).Value = (cab.CDG_VEND ?? "").PadLeft(3, '0');
        //        cmd.Parameters.Add("@CDG_CPAG", SqlDbType.Char, 3).Value = CDG_CPAG;
        //        cmd.Parameters.Add("@CDG_MON", SqlDbType.Char, 3).Value = CDG_MON_DEF;
        //        cmd.Parameters.Add("@FEC_PED", SqlDbType.SmallDateTime).Value = cab.FEC_PED;
        //        cmd.Parameters.Add("@NUM_OCOM", SqlDbType.Char, 60).Value = "";
        //        cmd.Parameters.Add("@IMP_STOT", SqlDbType.Decimal).Value = impStot;
        //        cmd.Parameters.Add("@IMP_TIGV", SqlDbType.Decimal).Value = impTigv;
        //        cmd.Parameters.Add("@IMP_TDCT", SqlDbType.Decimal).Value = IMP_TDCT_FIJO;
        //        cmd.Parameters.Add("@IMP_TTOT", SqlDbType.Decimal).Value = impTtot;
        //        cmd.Parameters.Add("@POR_TDCT", SqlDbType.Decimal).Value = POR_TDCT_FIJO;
        //        cmd.Parameters.Add("@POR_TIGV", SqlDbType.Decimal).Value = TAZA_IGV_100;
        //        cmd.Parameters.Add("@OBS_PED", SqlDbType.Text).Value = Nz(cab.OBS_PED);
        //        cmd.Parameters.Add("@SWT_PED", SqlDbType.Char, 1).Value = "";
        //        cmd.Parameters.Add("@RUC_CLI", SqlDbType.Char, 8).Value = "00000000";
        //        cmd.Parameters.Add("@SWT_COT", SqlDbType.Decimal).Value = 0;
        //        cmd.Parameters.Add("@SWT_PTV", SqlDbType.Char, 1).Value = SWT_PTV_FIJO;
        //        cmd.Parameters.Add("@ORI_AREA", SqlDbType.Char, 3).Value = ORI_AREA;
        //        cmd.Parameters.Add("@CDG_AREA", SqlDbType.Char, 3).Value = "";
        //        cmd.Parameters.Add("@NUM_COT", SqlDbType.Char, 8).Value = "";
        //        cmd.Parameters.Add("@CDG_USR", SqlDbType.Char, 10).Value = Nz(cab.CDG_USR);
        //        cmd.Parameters.Add("@CDG_PRIO", SqlDbType.Char, 3).Value = CDG_PRIO;
        //        cmd.Parameters.Add("@IMP_AJU", SqlDbType.Decimal).Value = 0.00m;
        //        cmd.Parameters.Add("@CDG_LOC", SqlDbType.Char, 3).Value = CDG_LOC_DEF;
        //        cmd.Parameters.Add("@SWT_DIST", SqlDbType.Decimal).Value = SWT_DIST_FIJO;
        //        cmd.Parameters.Add("@REF_PED", SqlDbType.Char, 100).Value = "";
        //        cmd.Parameters.Add("@SWT_PROD", SqlDbType.Char, 1).Value = "";
        //        cmd.Parameters.Add("@NUM_NSAL", SqlDbType.Char, 10).Value = "";
        //        cmd.Parameters.Add("@FEC_ENT", SqlDbType.SmallDateTime).Value = DBNull.Value;
        //        cmd.Parameters.Add("@NUM_MESA", SqlDbType.Char, 3).Value = Nz(cab.NUM_MESA).PadLeft(3, '0');
        //        cmd.Parameters.Add("@NUM_PERS", SqlDbType.Decimal).Value = (object)cab.NUM_PERS ?? DBNull.Value;
        //        cmd.Parameters.Add("@HRA_PED", SqlDbType.Char, 5).Value = DateTime.Now.ToString("HH:mm");
        //        cmd.Parameters.Add("@PND_APR", SqlDbType.Char, 1).Value = "";
        //        cmd.Parameters.Add("@FEC_APR", SqlDbType.DateTime).Value = DBNull.Value;
        //        cmd.Parameters.Add("@USR_APR", SqlDbType.Char, 10).Value = "";
        //        cmd.Parameters.Add("@HRA_APR", SqlDbType.Char, 5).Value = "";
        //        cmd.Parameters.Add("@VAL_DPVT", SqlDbType.Decimal).Value = VAL_DPVT;
        //        cmd.Parameters.Add("@POR_DPVT", SqlDbType.Decimal).Value = POR_DPVT;
        //        cmd.Parameters.Add("@DCT_APR", SqlDbType.Char, 10).Value = "";
        //        cmd.Parameters.Add("@CTA_IVA", SqlDbType.Char, 10).Value = "";
        //        cmd.Parameters.Add("@CTA_ICA", SqlDbType.Char, 10).Value = "";
        //        cmd.Parameters.Add("@CTA_FTE", SqlDbType.Char, 10).Value = "";
        //        cmd.Parameters.Add("@POR_IVA", SqlDbType.Decimal).Value = POR_IVA;
        //        cmd.Parameters.Add("@POR_ICA", SqlDbType.Decimal).Value = POR_ICA;
        //        cmd.Parameters.Add("@POR_FTE", SqlDbType.Decimal).Value = POR_FTE;
        //        cmd.Parameters.Add("@VAL_RET", SqlDbType.Decimal).Value = 0.00m;
        //        cmd.Parameters.Add("@VAL_IVA", SqlDbType.Decimal).Value = 0.00m;
        //        cmd.Parameters.Add("@VAL_ICA", SqlDbType.Decimal).Value = 0.00m;
        //        cmd.Parameters.Add("@VAL_FTE", SqlDbType.Decimal).Value = 0.00m;
        //        cmd.Parameters.Add("@FEC_ING", SqlDbType.SmallDateTime).Value = DBNull.Value;
        //        cmd.Parameters.Add("@FEC_SAL", SqlDbType.SmallDateTime).Value = DBNull.Value;
        //        cmd.Parameters.Add("@HRA_ING", SqlDbType.Char, 5).Value = "";
        //        cmd.Parameters.Add("@HRA_SAL", SqlDbType.Char, 5).Value = "";
        //        cmd.Parameters.Add("@TDC_CLI", SqlDbType.Char, 3).Value = "";
        //        cmd.Parameters.Add("@DOI_CLI", SqlDbType.Char, 15).Value = "";
        //        cmd.Parameters.Add("@CDG_NAC", SqlDbType.Char, 3).Value = "";
        //        cmd.Parameters.Add("@CNT_PED", SqlDbType.Char, 60).Value = "";
        //        cmd.Parameters.Add("@VAL_CARG", SqlDbType.Decimal).Value = VAL_CARG;
        //        cmd.Parameters.Add("@POR_CARG", SqlDbType.Decimal).Value = POR_CARG;
        //        cmd.Parameters.Add("@TIP_PTV", SqlDbType.Char, 1).Value = "2";
        //        cmd.Parameters.Add("@CDG_CAJA", SqlDbType.Char, 3).Value = Nz(cab.CDG_CAJA);
        //        cmd.Parameters.Add("@CDG_AMB", SqlDbType.Char, 3).Value = Nz(cab.CDG_AMB).PadLeft(3, '0');
        //        cmd.Parameters.Add("@IMP_TISC", SqlDbType.Decimal).Value = IMP_TISC_FIJO;

        //        cmd.Parameters["@IMP_STOT"].Precision = 15; cmd.Parameters["@IMP_STOT"].Scale = 2;
        //        cmd.Parameters["@IMP_TIGV"].Precision = 15; cmd.Parameters["@IMP_TIGV"].Scale = 2;
        //        cmd.Parameters["@IMP_TDCT"].Precision = 15; cmd.Parameters["@IMP_TDCT"].Scale = 2;
        //        cmd.Parameters["@IMP_TTOT"].Precision = 15; cmd.Parameters["@IMP_TTOT"].Scale = 2;
        //        cmd.Parameters["@POR_TDCT"].Precision = 7; cmd.Parameters["@POR_TDCT"].Scale = 2;
        //        cmd.Parameters["@POR_TIGV"].Precision = 7; cmd.Parameters["@POR_TIGV"].Scale = 2;
        //        cmd.Parameters["@NUM_PERS"].Precision = 3; cmd.Parameters["@NUM_PERS"].Scale = 0;

        //        cmd.ExecuteNonQuery();
        //    }
        //}

        private static void InsertarCabecera(SqlConnection cn, SqlTransaction tx, ceMPedido cab)
        {
            const string CDG_CPAG = "001";
            const string CDG_MON_DEF = "001";
            const string SWT_PTV_FIJO = "S";
            const string ORI_AREA = "001";
            const string CDG_PRIO = "000";
            const string CDG_LOC_DEF = "000";

            const decimal IMP_TDCT_FIJO = 0.00m, POR_TDCT_FIJO = 0.00m, IMP_TISC_FIJO = 0.00m;
            const int SWT_DIST_FIJO = 0;
            const decimal VAL_DPVT = 0.00m, POR_DPVT = 0.00m, POR_IVA = 0.00m, POR_ICA = 0.00m, POR_FTE = 0.00m, VAL_CARG = 0.00m, POR_CARG = 0.00m;

            // Se recalculan al final desde D_PEDIDO
            decimal impStot = 0m, impTigv = 0m, impTtot = 0m;

            string sql = @"
    INSERT INTO dbo.M_PEDIDO(
        NUM_PED, CDG_VEND, CDG_CPAG, CDG_MON, FEC_PED,
        NUM_OCOM, IMP_STOT, IMP_TIGV, IMP_TDCT, IMP_TTOT, POR_TDCT, POR_TIGV,
        OBS_PED, SWT_PED, RUC_CLI, SWT_COT, FEC_ANUL, SWT_PTV, ORI_AREA, CDG_AREA, NUM_COT,
        CDG_USR, CDG_PRIO, IMP_AJU, CDG_LOC, SWT_DIST, REF_PED, SWT_PROD,
        NUM_NSAL, FEC_ENT, NUM_MESA, NUM_PERS, HRA_PED, PND_APR, FEC_APR, USR_APR, HRA_APR,
        VAL_DPVT, POR_DPVT, DCT_APR, CTA_IVA, CTA_ICA, CTA_FTE, POR_IVA, POR_ICA, POR_FTE,
        VAL_RET, VAL_IVA, VAL_ICA, VAL_FTE,
        FEC_ING, FEC_SAL, HRA_ING, HRA_SAL,
        TDC_CLI, DOI_CLI, CDG_NAC, CNT_PED, VAL_CARG, POR_CARG,
        TIP_PTV, CDG_CAJA, CDG_AMB, IMP_TISC)
    VALUES(
        @NUM_PED, @CDG_VEND, @CDG_CPAG, @CDG_MON, @FEC_PED,
        @NUM_OCOM, @IMP_STOT, @IMP_TIGV, @IMP_TDCT, @IMP_TTOT, @POR_TDCT, @POR_TIGV,
        @OBS_PED, @SWT_PED, @RUC_CLI, @SWT_COT, @FEC_ANUL, @SWT_PTV, @ORI_AREA, @CDG_AREA, @NUM_COT,
        @CDG_USR, @CDG_PRIO, @IMP_AJU, @CDG_LOC, @SWT_DIST, @REF_PED, @SWT_PROD,
        @NUM_NSAL, @FEC_ENT, @NUM_MESA, @NUM_PERS, @HRA_PED, @PND_APR, @FEC_APR, @USR_APR, @HRA_APR,
        @VAL_DPVT, @POR_DPVT, @DCT_APR, @CTA_IVA, @CTA_ICA, @CTA_FTE, @POR_IVA, @POR_ICA, @POR_FTE,
        @VAL_RET, @VAL_IVA, @VAL_ICA, @VAL_FTE,
        @FEC_ING, @FEC_SAL, @HRA_ING, @HRA_SAL,
        @TDC_CLI, @DOI_CLI, @CDG_NAC, @CNT_PED, @VAL_CARG, @POR_CARG,
        @TIP_PTV, @CDG_CAJA, @CDG_AMB, @IMP_TISC);";

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
                cmd.Parameters.Add("@IMP_TDCT", SqlDbType.Decimal).Value = IMP_TDCT_FIJO;
                cmd.Parameters.Add("@IMP_TTOT", SqlDbType.Decimal).Value = impTtot;
                cmd.Parameters.Add("@POR_TDCT", SqlDbType.Decimal).Value = POR_TDCT_FIJO;
                cmd.Parameters.Add("@POR_TIGV", SqlDbType.Decimal).Value = TAZA_IGV_100;

                cmd.Parameters.Add("@OBS_PED", SqlDbType.Text).Value = Nz(cab.OBS_PED);
                cmd.Parameters.Add("@SWT_PED", SqlDbType.Char, 1).Value = "";
                cmd.Parameters.Add("@RUC_CLI", SqlDbType.Char, 8).Value = "00000000";
                cmd.Parameters.Add("@SWT_COT", SqlDbType.Decimal).Value = 0;

                // *** NUEVO: FEC_ANUL NULL para compatibilidad con app nativa ***
                cmd.Parameters.Add("@FEC_ANUL", SqlDbType.DateTime).Value = DBNull.Value;

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
                cmd.Parameters.Add("@NUM_PERS", SqlDbType.Decimal).Value = (object)cab.NUM_PERS ?? DBNull.Value;
                cmd.Parameters.Add("@HRA_PED", SqlDbType.Char, 5).Value = DateTime.Now.ToString("HH:mm");
                cmd.Parameters.Add("@PND_APR", SqlDbType.Char, 1).Value = "";
                cmd.Parameters.Add("@FEC_APR", SqlDbType.DateTime).Value = DBNull.Value;
                cmd.Parameters.Add("@USR_APR", SqlDbType.Char, 10).Value = "";
                cmd.Parameters.Add("@HRA_APR", SqlDbType.Char, 5).Value = "";

                cmd.Parameters.Add("@VAL_DPVT", SqlDbType.Decimal).Value = VAL_DPVT;
                cmd.Parameters.Add("@POR_DPVT", SqlDbType.Decimal).Value = POR_DPVT;
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
                cmd.Parameters.Add("@VAL_CARG", SqlDbType.Decimal).Value = VAL_CARG;
                cmd.Parameters.Add("@POR_CARG", SqlDbType.Decimal).Value = POR_CARG;

                cmd.Parameters.Add("@TIP_PTV", SqlDbType.Char, 1).Value = "2";
                cmd.Parameters.Add("@CDG_CAJA", SqlDbType.Char, 3).Value = Nz(cab.CDG_CAJA);
                cmd.Parameters.Add("@CDG_AMB", SqlDbType.Char, 3).Value = Nz(cab.CDG_AMB).PadLeft(3, '0');
                cmd.Parameters.Add("@IMP_TISC", SqlDbType.Decimal).Value = IMP_TISC_FIJO;

                // Escalas
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


        //private static void InsertarDetalles(SqlConnection cn, SqlTransaction tx, ceMPedido cab, Func<string, string> resolverImpresora, Func<string, Tuple<decimal?, bool?>> resolverTrib)
        //        {
        //            const string SQL = @"
        //            INSERT INTO dbo.D_PEDIDO(
        //                NUM_PED, CDG_PROD, CDG_FPRD, CAN_PPRD, PRE_PPRD, DCT_PPRD, DCT_FIC, IGV_PPRD, IMP_TPRD,
        //                CAN_DPRD, CAN_FPRD, OBS_PPRD, CDG_LPRC, PRE_IGV, IMP_IGV,
        //                CDG_PROM, FAC_UVTA, CDG_UVTA, COM_PPRD, CAN_PROD, CAN_OTRB, CAN_UVTA, PRE_UVTA, VAL_UVTA, TOT_UVTA,
        //                POR_TISC, swt_igv, com_impo, SAC_PPRD, SWT_CMP, POR_IGV, IMP_IVA, NUM_ITEM, IMP_PROD, SWT_IMPR,
        //                PCT_CARG, IMP_CARG, ORI_PED, CDG_COMB, TDC_COMA, DOC_COMA)
        //            VALUES(
        //                @NUM_PED, @CDG_PROD, @CDG_FPRD, @CAN_PPRD, @PRE_PPRD, @DCT_PPRD, @DCT_FIC, @IGV_PPRD, @IMP_TPRD,
        //                @CAN_DPRD, @CAN_FPRD, @OBS_PPRD, @CDG_LPRC, @PRE_IGV, @IMP_IGV,
        //                @CDG_PROM, @FAC_UVTA, @CDG_UVTA, @COM_PPRD, @CAN_PROD, @CAN_OTRB, @CAN_UVTA, @PRE_UVTA, @VAL_UVTA, @TOT_UVTA,
        //                @POR_TISC, @swt_igv, @com_impo, @SAC_PPRD, @SWT_CMP, @POR_IGV, @IMP_IVA, @NUM_ITEM, @IMP_PROD, @SWT_IMPR,
        //                @PCT_CARG, @IMP_CARG, @ORI_PED, @CDG_COMB, @TDC_COMA, @DOC_COMA);";

        //            int item = 0;
        //            using (var cmd = new SqlCommand(SQL, cn, tx))
        //            {
        //                foreach (var d in cab.Detalles)
        //                {
        //                    item++;
        //                    string cod10 = To10(d.COD10 ?? d.CDG_PROD.ToString(CultureInfo.InvariantCulture));
        //                    decimal can = Round4(d.CAN_PPRD);

        //                    // Precio con IGV => base + IGV
        //                    decimal pConIgv = Round4(d.PRE_IGV > 0 ? d.PRE_IGV : d.PRE_PPRD * UNO_MAS_IGV);
        //                    decimal subConIgv = Round2(pConIgv * can);
        //                    decimal baseLinea = Round2(subConIgv / UNO_MAS_IGV);
        //                    decimal igvLinea = Round2(subConIgv - baseLinea);
        //                    decimal pSinIgv = can == 0 ? 0m : Round4(baseLinea / can);

        //                    string notas = Nz(d.OBS_PPRD);
        //                    string lprc = (d.CDG_LPRC <= 0) ? "001" : d.CDG_LPRC.ToString("000");

        //                    string swt_igv_text;
        //                    bool hasSwt = false;
        //                    if (resolverTrib != null)
        //                    {
        //                        var t = resolverTrib(cod10);
        //                        if (t != null && t.Item2.HasValue) { hasSwt = true; swt_igv_text = t.Item2.Value ? "X" : ""; }
        //                        else swt_igv_text = "";
        //                    }
        //                    else swt_igv_text = "";

        //                    if (!hasSwt) swt_igv_text = GetSwtIgvFromProducto(cn, tx, cod10) ? "X" : "";

        //                    string imp = ObtenerImpresoraParaProducto(cn, tx, cod10, resolverImpresora);
        //                    string swtImpr = string.IsNullOrWhiteSpace(imp) ? "" : "X";

        //                    string combDb = NormalizeCombForDb(GetStrPropOrEmpty(d, "CDG_COMB"));

        //                    cmd.Parameters.Clear();
        //                    cmd.Parameters.Add("@NUM_PED", SqlDbType.Char, 8).Value = To8(cab.NUM_PED);
        //                    cmd.Parameters.Add("@CDG_PROD", SqlDbType.Char, 10).Value = cod10;
        //                    cmd.Parameters.Add("@CDG_FPRD", SqlDbType.Char, 3).Value = "000";
        //                    cmd.Parameters.Add("@CAN_PPRD", SqlDbType.Decimal).Value = can;
        //                    cmd.Parameters.Add("@PRE_PPRD", SqlDbType.Decimal).Value = pSinIgv;
        //                    cmd.Parameters.Add("@DCT_PPRD", SqlDbType.Decimal).Value = 0.00m;
        //                    cmd.Parameters.Add("@DCT_FIC", SqlDbType.Decimal).Value = 0.00m;
        //                    cmd.Parameters.Add("@IGV_PPRD", SqlDbType.Decimal).Value = 0.00m;
        //                    cmd.Parameters.Add("@IMP_TPRD", SqlDbType.Decimal).Value = baseLinea;
        //                    cmd.Parameters.Add("@CAN_DPRD", SqlDbType.Decimal).Value = 0.0000m;
        //                    cmd.Parameters.Add("@CAN_FPRD", SqlDbType.Decimal).Value = 0.0000m;
        //                    cmd.Parameters.Add("@OBS_PPRD", SqlDbType.Text).Value = notas;
        //                    cmd.Parameters.Add("@CDG_LPRC", SqlDbType.Char, 3).Value = lprc;
        //                    cmd.Parameters.Add("@PRE_IGV", SqlDbType.Decimal).Value = pConIgv;
        //                    cmd.Parameters.Add("@IMP_IGV", SqlDbType.Decimal).Value = igvLinea;
        //                    cmd.Parameters.Add("@CDG_PROM", SqlDbType.Char, 10).Value = "";
        //                    cmd.Parameters.Add("@FAC_UVTA", SqlDbType.Decimal).Value = Round10(1.0000000000m);
        //                    cmd.Parameters.Add("@CDG_UVTA", SqlDbType.Char, 3).Value = "001";
        //                    cmd.Parameters.Add("@COM_PPRD", SqlDbType.Decimal).Value = 0.00m;
        //                    cmd.Parameters.Add("@CAN_PROD", SqlDbType.Decimal).Value = 0.0000m;
        //                    cmd.Parameters.Add("@CAN_OTRB", SqlDbType.Decimal).Value = 0.0000m;
        //                    cmd.Parameters.Add("@CAN_UVTA", SqlDbType.Decimal).Value = can;
        //                    cmd.Parameters.Add("@PRE_UVTA", SqlDbType.Decimal).Value = pConIgv;
        //                    cmd.Parameters.Add("@VAL_UVTA", SqlDbType.Decimal).Value = pSinIgv;
        //                    cmd.Parameters.Add("@TOT_UVTA", SqlDbType.Decimal).Value = subConIgv;
        //                    cmd.Parameters.Add("@POR_TISC", SqlDbType.Decimal).Value = 0.00m;
        //                    cmd.Parameters.Add("@swt_igv", SqlDbType.Char, 1).Value = swt_igv_text;
        //                    cmd.Parameters.Add("@com_impo", SqlDbType.Decimal).Value = 0.00m;
        //                    cmd.Parameters.Add("@SAC_PPRD", SqlDbType.Char, 15).Value = "";
        //                    cmd.Parameters.Add("@SWT_CMP", SqlDbType.Char, 1).Value = "";
        //                    cmd.Parameters.Add("@POR_IGV", SqlDbType.Decimal).Value = TAZA_IGV_100;
        //                    cmd.Parameters.Add("@IMP_IVA", SqlDbType.Decimal).Value = 0.00m;
        //                    cmd.Parameters.Add("@NUM_ITEM", SqlDbType.Char, 5).Value = item.ToString("00000", CultureInfo.InvariantCulture);
        //                    cmd.Parameters.Add("@IMP_PROD", SqlDbType.Char, 3).Value = Nz(imp);
        //                    cmd.Parameters.Add("@SWT_IMPR", SqlDbType.Char, 1).Value = swtImpr;
        //                    cmd.Parameters.Add("@PCT_CARG", SqlDbType.Decimal).Value = 0.00m;
        //                    cmd.Parameters.Add("@IMP_CARG", SqlDbType.Decimal).Value = 0.00m;
        //                    cmd.Parameters.Add("@ORI_PED", SqlDbType.Char, 8).Value = "";
        //                    cmd.Parameters.Add("@CDG_COMB", SqlDbType.Char, 10).Value = combDb;
        //                    cmd.Parameters.Add("@TDC_COMA", SqlDbType.Char, 3).Value = "";
        //                    cmd.Parameters.Add("@DOC_COMA", SqlDbType.Char, 10).Value = "";

        //                    cmd.Parameters["@CAN_PPRD"].Precision = 15; cmd.Parameters["@CAN_PPRD"].Scale = 4;
        //                    cmd.Parameters["@PRE_PPRD"].Precision = 15; cmd.Parameters["@PRE_PPRD"].Scale = 4;
        //                    cmd.Parameters["@IMP_TPRD"].Precision = 15; cmd.Parameters["@IMP_TPRD"].Scale = 2;
        //                    cmd.Parameters["@PRE_IGV"].Precision = 15; cmd.Parameters["@PRE_IGV"].Scale = 4;
        //                    cmd.Parameters["@IMP_IGV"].Precision = 15; cmd.Parameters["@IMP_IGV"].Scale = 2;
        //                    cmd.Parameters["@FAC_UVTA"].Precision = 15; cmd.Parameters["@FAC_UVTA"].Scale = 10;
        //                    cmd.Parameters["@CAN_UVTA"].Precision = 15; cmd.Parameters["@CAN_UVTA"].Scale = 4;
        //                    cmd.Parameters["@PRE_UVTA"].Precision = 15; cmd.Parameters["@PRE_UVTA"].Scale = 4;
        //                    cmd.Parameters["@VAL_UVTA"].Precision = 15; cmd.Parameters["@VAL_UVTA"].Scale = 4;
        //                    cmd.Parameters["@TOT_UVTA"].Precision = 15; cmd.Parameters["@TOT_UVTA"].Scale = 2;
        //                    cmd.Parameters["@POR_TISC"].Precision = 7; cmd.Parameters["@POR_TISC"].Scale = 2;
        //                    cmd.Parameters["@com_impo"].Precision = 15; cmd.Parameters["@com_impo"].Scale = 2;
        //                    cmd.Parameters["@POR_IGV"].Precision = 7; cmd.Parameters["@POR_IGV"].Scale = 2;
        //                    cmd.Parameters["@IMP_IVA"].Precision = 15; cmd.Parameters["@IMP_IVA"].Scale = 2;

        //                    cmd.ExecuteNonQuery();
        //                }
        //            }
        //        }

        ////////////private static decimal GetPrecioBaseValSol(SqlConnection cn, SqlTransaction tx, string cod10, string cdgLprc = "001")
        ////////////{
        ////////////    using (var c = new SqlCommand(
        ////////////        @"SELECT ISNULL(MAX(CAST(VAL_SOL AS decimal(15,4))), 0)
        ////////////  FROM dbo.M_PRECIO
        ////////////  WHERE CDG_LPRC = @L AND CDG_PROD = @P;", cn, tx))
        ////////////    {
        ////////////        c.Parameters.Add("@L", SqlDbType.Char, 3).Value = (cdgLprc ?? "001").PadLeft(3, '0');
        ////////////        c.Parameters.Add("@P", SqlDbType.Char, 10).Value = To10(cod10);
        ////////////        object o = c.ExecuteScalar();
        ////////////        return (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
        ////////////    }
        ////////////}

        private static void InsertarDetalles(
            SqlConnection cn,
            SqlTransaction tx,
            ceMPedido cab,
            Func<string, string> resolverImpresora,
            Func<string, Tuple<decimal?, bool?>> resolverTrib)
        {
            const string SQL = @"
                INSERT INTO dbo.D_PEDIDO(
                    NUM_PED, CDG_PROD, CDG_FPRD, CAN_PPRD, PRE_PPRD, DCT_PPRD, DCT_FIC, IGV_PPRD, IMP_TPRD,
                    CAN_DPRD, CAN_FPRD, OBS_PPRD, CDG_LPRC, PRE_IGV, IMP_IGV, CDG_PROM, FAC_UVTA, CDG_UVTA,
                    COM_PPRD, CAN_PROD, CAN_OTRB, CAN_UVTA, PRE_UVTA, VAL_UVTA, TOT_UVTA, POR_TISC, swt_igv,
                    com_impo, SAC_PPRD, SWT_CMP, POR_IGV, IMP_IVA, NUM_ITEM, IMP_PROD, SWT_IMPR, PCT_CARG,
                    IMP_CARG, ORI_PED, CDG_COMB, TDC_COMA, DOC_COMA
                )
                VALUES(
                    @NUM_PED, @CDG_PROD, @CDG_FPRD, @CAN_PPRD, @PRE_PPRD, @DCT_PPRD, @DCT_FIC, @IGV_PPRD, @IMP_TPRD,
                    @CAN_DPRD, @CAN_FPRD, @OBS_PPRD, @CDG_LPRC, @PRE_IGV, @IMP_IGV, @CDG_PROM, @FAC_UVTA, @CDG_UVTA,
                    @COM_PPRD, @CAN_PROD, @CAN_OTRB, @CAN_UVTA, @PRE_UVTA, @VAL_UVTA, @TOT_UVTA, @POR_TISC, @swt_igv,
                    @com_impo, @SAC_PPRD, @SWT_CMP, @POR_IGV, @IMP_IVA, @NUM_ITEM, @IMP_PROD, @SWT_IMPR, @PCT_CARG,
                    @IMP_CARG, @ORI_PED, @CDG_COMB, @TDC_COMA, @DOC_COMA
                );";

            int item = 0;

            using (var cmd = new SqlCommand(SQL, cn, tx))
            {
                foreach (var d in cab.Detalles)
                {
                    item++;

                    // --- Datos base ---
                    string cod10 = To10(d.COD10 ?? d.CDG_PROD.ToString(CultureInfo.InvariantCulture));
                    decimal can = Round4(d.CAN_PPRD);

                    // Precio con IGV (unitario). Si no viene, calcular desde PRE_PPRD * (1 + IGV)
                    decimal pConIgv = Round4(d.PRE_IGV > 0 ? d.PRE_IGV : d.PRE_PPRD * UNO_MAS_IGV);

                    // Regla: redondear por unidad primero
                    decimal pSinIgv = Round4(pConIgv / UNO_MAS_IGV);      // ej. 25 / 1.10 = 22.7273
                    decimal baseLinea = Round2(pSinIgv * can);              // total sin IGV
                    decimal igvLinea = Round2((pConIgv - pSinIgv) * can);  // total IGV

                    string notas = Nz(d.OBS_PPRD);
                    string lprc = (d.CDG_LPRC <= 0) ? "001" : d.CDG_LPRC.ToString("000");

                    // swt_igv: intentar por resolverTrib, si no, leer de producto
                    string swt_igv_text = "";
                    bool hasSwt = false;

                    if (resolverTrib != null)
                    {
                        var t = resolverTrib(cod10);
                        if (t != null && t.Item2.HasValue)
                        {
                            hasSwt = true;
                            swt_igv_text = t.Item2.Value ? "X" : "";
                        }
                    }

                    if (!hasSwt)
                    {
                        swt_igv_text = GetSwtIgvFromProducto(cn, tx, cod10) ? "X" : "";
                    }

                    // Impresora
                    string imp = ObtenerImpresoraParaProducto(cn, tx, cod10, resolverImpresora);
                    string swtImpr = string.IsNullOrWhiteSpace(imp) ? "" : "X";

                    // Combo (normalizado)
                    string combDb = NormalizeCombForDb(GetStrPropOrEmpty(d, "CDG_COMB"));

                    // --- Parámetros ---
                    cmd.Parameters.Clear();

                    cmd.Parameters.Add("@NUM_PED", SqlDbType.Char, 8).Value = To8(cab.NUM_PED);
                    cmd.Parameters.Add("@CDG_PROD", SqlDbType.Char, 10).Value = cod10;
                    cmd.Parameters.Add("@CDG_FPRD", SqlDbType.Char, 3).Value = "000";

                    cmd.Parameters.Add("@CAN_PPRD", SqlDbType.Decimal).Value = can;
                    cmd.Parameters.Add("@PRE_PPRD", SqlDbType.Decimal).Value = 0.0000m;
                    cmd.Parameters.Add("@DCT_PPRD", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@DCT_FIC", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@IGV_PPRD", SqlDbType.Decimal).Value = 0.00m;

                    // Totales sin IGV e IGV total
                    cmd.Parameters.Add("@IMP_TPRD", SqlDbType.Decimal).Value = baseLinea;
                    cmd.Parameters.Add("@CAN_DPRD", SqlDbType.Decimal).Value = 0.0000m;
                    cmd.Parameters.Add("@CAN_FPRD", SqlDbType.Decimal).Value = 0.0000m;

                    cmd.Parameters.Add("@OBS_PPRD", SqlDbType.Text).Value = notas;
                    cmd.Parameters.Add("@CDG_LPRC", SqlDbType.Char, 3).Value = lprc;

                    // Unitario con IGV + IGV total
                    cmd.Parameters.Add("@PRE_IGV", SqlDbType.Decimal).Value = pConIgv;
                    cmd.Parameters.Add("@IMP_IGV", SqlDbType.Decimal).Value = igvLinea;

                    cmd.Parameters.Add("@CDG_PROM", SqlDbType.Char, 10).Value = "";
                    cmd.Parameters.Add("@FAC_UVTA", SqlDbType.Decimal).Value = Round10(1.0000000000m);
                    cmd.Parameters.Add("@CDG_UVTA", SqlDbType.Char, 3).Value = "001";
                    cmd.Parameters.Add("@COM_PPRD", SqlDbType.Decimal).Value = 0.00m;

                    cmd.Parameters.Add("@CAN_PROD", SqlDbType.Decimal).Value = 0.0000m;
                    cmd.Parameters.Add("@CAN_OTRB", SqlDbType.Decimal).Value = 0.0000m;

                    // Cantidad y precios/valores en unidad de venta
                    cmd.Parameters.Add("@CAN_UVTA", SqlDbType.Decimal).Value = can;
                    cmd.Parameters.Add("@PRE_UVTA", SqlDbType.Decimal).Value = pConIgv; // unitario con IGV
                    cmd.Parameters.Add("@VAL_UVTA", SqlDbType.Decimal).Value = pSinIgv; // unitario sin IGV
                    cmd.Parameters.Add("@TOT_UVTA", SqlDbType.Decimal).Value = baseLinea; // total sin IGV

                    cmd.Parameters.Add("@POR_TISC", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@swt_igv", SqlDbType.Char, 1).Value = swt_igv_text;
                    cmd.Parameters.Add("@com_impo", SqlDbType.Decimal).Value = 0.00m;

                    cmd.Parameters.Add("@SAC_PPRD", SqlDbType.Char, 15).Value = "";
                    cmd.Parameters.Add("@SWT_CMP", SqlDbType.Char, 1).Value = "";
                    cmd.Parameters.Add("@POR_IGV", SqlDbType.Decimal).Value = TAZA_IGV_100; // p.ej. 10.00
                    cmd.Parameters.Add("@IMP_IVA", SqlDbType.Decimal).Value = 0.00m;

                    cmd.Parameters.Add("@NUM_ITEM", SqlDbType.Char, 5).Value = item.ToString("00000", CultureInfo.InvariantCulture);
                    cmd.Parameters.Add("@IMP_PROD", SqlDbType.Char, 3).Value = Nz(imp);
                    cmd.Parameters.Add("@SWT_IMPR", SqlDbType.Char, 1).Value = swtImpr;

                    cmd.Parameters.Add("@PCT_CARG", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@IMP_CARG", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@ORI_PED", SqlDbType.Char, 8).Value = "";
                    cmd.Parameters.Add("@CDG_COMB", SqlDbType.Char, 10).Value = combDb;
                    cmd.Parameters.Add("@TDC_COMA", SqlDbType.Char, 3).Value = "";
                    cmd.Parameters.Add("@DOC_COMA", SqlDbType.Char, 10).Value = "";

                    // --- Precisión / escala ---
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

                    // Ejecutar
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ======== Anexar a pedido existente ========
        public void AnexarSoloDetalles(string numPed8, IList<ceDPedido> nuevos, Func<string, string> resolverImpresora, Func<string, Tuple<decimal?, bool?>> resolverTrib)
        {
            if (string.IsNullOrWhiteSpace(numPed8)) throw new ArgumentException("NUM_PED vacío.", nameof(numPed8));
            if (nuevos == null || nuevos.Count == 0) return;

            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        AsegurarExistePedido(cn, tx, numPed8, true);
                        InsertarDetallesEnPedidoExistente(cn, tx, numPed8, nuevos, resolverImpresora, resolverTrib);
                        RecalcularTotalesCabeceraDesdeDetalle(cn, tx, numPed8);
                        using (var cmdF = new SqlCommand("UPDATE dbo.M_PEDIDO SET FEC_PED = CASE WHEN FEC_PED IS NULL THEN GETDATE() ELSE FEC_PED END WHERE NUM_PED=@P;", cn, tx))
                        { cmdF.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8); cmdF.ExecuteNonQuery(); }
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        private static void AsegurarExistePedido(SqlConnection cn, SqlTransaction tx, string numPed8, bool debeAbierto)
        {
            using (var c = new SqlCommand("SELECT COUNT(1) FROM dbo.M_PEDIDO WHERE NUM_PED=@P;", cn, tx))
            { c.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8); if (Convert.ToInt32(c.ExecuteScalar()) == 0) throw new InvalidOperationException($"El pedido {To8(numPed8)} no existe."); }
            if (debeAbierto)
            {
                using (var c = new SqlCommand("SELECT CASE WHEN (SWT_PED IS NULL OR LTRIM(RTRIM(SWT_PED))='') THEN 1 ELSE 0 END FROM dbo.M_PEDIDO WHERE NUM_PED=@P;", cn, tx))
                { c.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8); if (Convert.ToInt32(c.ExecuteScalar()) != 1) throw new InvalidOperationException($"El pedido {To8(numPed8)} ya está cerrado/anulado."); }
            }
        }
        private static int ObtenerMaxNumItem(SqlConnection cn, SqlTransaction tx, string numPed8)
        {
            using (var c = new SqlCommand("SELECT ISNULL(MAX(CONVERT(INT, NULLIF(NUM_ITEM,''))),0) FROM dbo.D_PEDIDO WHERE NUM_PED=@P;", cn, tx))
            { c.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8); return Convert.ToInt32(c.ExecuteScalar() ?? 0); }
        }
        private static int ObtenerMaxCdgComb(SqlConnection cn, SqlTransaction tx, string numPed8)
        {
            using (var c = new SqlCommand("SELECT ISNULL(MAX(CONVERT(INT, NULLIF(CDG_COMB,''))),0) FROM dbo.D_PEDIDO WHERE NUM_PED=@P;", cn, tx))
            { c.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8); return Convert.ToInt32(c.ExecuteScalar() ?? 0); }
        }

        //private static void InsertarDetallesEnPedidoExistente(SqlConnection cn, SqlTransaction tx, string numPed8, IList<ceDPedido> nuevos, Func<string, string> resolverImpresora, Func<string, Tuple<decimal?, bool?>> resolverTrib)
        //{
        //    const string SQL = @"
        //    INSERT INTO dbo.D_PEDIDO(
        //        NUM_PED, CDG_PROD, CDG_FPRD, CAN_PPRD, PRE_PPRD, DCT_PPRD, DCT_FIC, IGV_PPRD, IMP_TPRD,
        //        CAN_DPRD, CAN_FPRD, OBS_PPRD, CDG_LPRC, PRE_IGV, IMP_IGV,
        //        CDG_PROM, FAC_UVTA, CDG_UVTA, COM_PPRD, CAN_PROD, CAN_OTRB, CAN_UVTA, PRE_UVTA, VAL_UVTA, TOT_UVTA,
        //        POR_TISC, swt_igv, com_impo, SAC_PPRD, SWT_CMP, POR_IGV, IMP_IVA, NUM_ITEM, IMP_PROD, SWT_IMPR,
        //        PCT_CARG, IMP_CARG, ORI_PED, CDG_COMB, TDC_COMA, DOC_COMA)
        //    VALUES(
        //        @NUM_PED, @CDG_PROD, @CDG_FPRD, @CAN_PPRD, @PRE_PPRD, @DCT_PPRD, @DCT_FIC, @IGV_PPRD, @IMP_TPRD,
        //        @CAN_DPRD, @CAN_FPRD, @OBS_PPRD, @CDG_LPRC, @PRE_IGV, @IMP_IGV,
        //        @CDG_PROM, @FAC_UVTA, @CDG_UVTA, @COM_PPRD, @CAN_PROD, @CAN_OTRB, @CAN_UVTA, @PRE_UVTA, @VAL_UVTA, @TOT_UVTA,
        //        @POR_TISC, @swt_igv, @com_impo, @SAC_PPRD, @SWT_CMP, @POR_IGV, @IMP_IVA, @NUM_ITEM, @IMP_PROD, @SWT_IMPR,
        //        @PCT_CARG, @IMP_CARG, @ORI_PED, @CDG_COMB, @TDC_COMA, @DOC_COMA);";

        //    int nextItem = ObtenerMaxNumItem(cn, tx, numPed8);
        //    int maxComb = Math.Max(ObtenerMaxCdgComb(cn, tx, numPed8), 99);
        //    var mapComb = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        //    using (var cmd = new SqlCommand(SQL, cn, tx))
        //    {
        //        foreach (var d in nuevos)
        //        {
        //            nextItem++;
        //            string cod10 = To10(d.COD10 ?? d.CDG_PROD.ToString(CultureInfo.InvariantCulture));
        //            decimal can = Round4(d.CAN_PPRD);

        //            decimal pConIgv = Round4(d.PRE_IGV > 0 ? d.PRE_IGV : d.PRE_PPRD * UNO_MAS_IGV);
        //            decimal subConIgv = Round2(pConIgv * can);
        //            decimal baseLinea = Round2(subConIgv / UNO_MAS_IGV);
        //            decimal igvLinea = Round2(subConIgv - baseLinea);
        //            decimal pSinIgv = can == 0 ? 0m : Round4(baseLinea / can);

        //            string notas = Nz(d.OBS_PPRD);
        //            string lprc = (d.CDG_LPRC <= 0) ? "001" : d.CDG_LPRC.ToString("000");

        //            string swtIgv;
        //            bool hasSwt = false;
        //            if (resolverTrib != null)
        //            {
        //                var t = resolverTrib(cod10);
        //                if (t != null && t.Item2.HasValue) { hasSwt = true; swtIgv = t.Item2.Value ? "X" : ""; }
        //                else swtIgv = "";
        //            }
        //            else swtIgv = "";
        //            if (!hasSwt) swtIgv = GetSwtIgvFromProducto(cn, tx, cod10) ? "X" : "";

        //            string imp = ObtenerImpresoraParaProducto(cn, tx, cod10, resolverImpresora);
        //            string swtImpr = string.IsNullOrWhiteSpace(imp) ? "" : "X";

        //            // remap CDG_COMB local->db
        //            string combInput = GetStrPropOrEmpty(d, "CDG_COMB");
        //            string combDb = "";
        //            if (!string.IsNullOrWhiteSpace(combInput))
        //            {
        //                if (!mapComb.TryGetValue(combInput, out int asign))
        //                {
        //                    asign = ++maxComb;
        //                    mapComb[combInput] = asign;
        //                }
        //                combDb = To10(asign.ToString());
        //            }

        //            cmd.Parameters.Clear();
        //            cmd.Parameters.Add("@NUM_PED", SqlDbType.Char, 8).Value = To8(numPed8);
        //            cmd.Parameters.Add("@CDG_PROD", SqlDbType.Char, 10).Value = cod10;
        //            cmd.Parameters.Add("@CDG_FPRD", SqlDbType.Char, 3).Value = "000";
        //            cmd.Parameters.Add("@CAN_PPRD", SqlDbType.Decimal).Value = can;
        //            cmd.Parameters.Add("@PRE_PPRD", SqlDbType.Decimal).Value = pSinIgv;
        //            cmd.Parameters.Add("@DCT_PPRD", SqlDbType.Decimal).Value = 0.00m;
        //            cmd.Parameters.Add("@DCT_FIC", SqlDbType.Decimal).Value = 0.00m;
        //            cmd.Parameters.Add("@IGV_PPRD", SqlDbType.Decimal).Value = 0.00m;
        //            cmd.Parameters.Add("@IMP_TPRD", SqlDbType.Decimal).Value = baseLinea;
        //            cmd.Parameters.Add("@CAN_DPRD", SqlDbType.Decimal).Value = 0.0000m;
        //            cmd.Parameters.Add("@CAN_FPRD", SqlDbType.Decimal).Value = 0.0000m;
        //            cmd.Parameters.Add("@OBS_PPRD", SqlDbType.Text).Value = notas;
        //            cmd.Parameters.Add("@CDG_LPRC", SqlDbType.Char, 3).Value = lprc;
        //            cmd.Parameters.Add("@PRE_IGV", SqlDbType.Decimal).Value = pConIgv;
        //            cmd.Parameters.Add("@IMP_IGV", SqlDbType.Decimal).Value = igvLinea;
        //            cmd.Parameters.Add("@CDG_PROM", SqlDbType.Char, 10).Value = "";
        //            cmd.Parameters.Add("@FAC_UVTA", SqlDbType.Decimal).Value = Round10(1.0000000000m);
        //            cmd.Parameters.Add("@CDG_UVTA", SqlDbType.Char, 3).Value = "001";
        //            cmd.Parameters.Add("@COM_PPRD", SqlDbType.Decimal).Value = 0.00m;
        //            cmd.Parameters.Add("@CAN_PROD", SqlDbType.Decimal).Value = 0.0000m;
        //            cmd.Parameters.Add("@CAN_OTRB", SqlDbType.Decimal).Value = 0.0000m;
        //            cmd.Parameters.Add("@CAN_UVTA", SqlDbType.Decimal).Value = can;
        //            cmd.Parameters.Add("@PRE_UVTA", SqlDbType.Decimal).Value = pConIgv;
        //            cmd.Parameters.Add("@VAL_UVTA", SqlDbType.Decimal).Value = pSinIgv;
        //            cmd.Parameters.Add("@TOT_UVTA", SqlDbType.Decimal).Value = subConIgv;
        //            cmd.Parameters.Add("@POR_TISC", SqlDbType.Decimal).Value = 0.00m;
        //            cmd.Parameters.Add("@swt_igv", SqlDbType.Char, 1).Value = swtIgv;
        //            cmd.Parameters.Add("@com_impo", SqlDbType.Decimal).Value = 0.00m;
        //            cmd.Parameters.Add("@SAC_PPRD", SqlDbType.Char, 15).Value = "";
        //            cmd.Parameters.Add("@SWT_CMP", SqlDbType.Char, 1).Value = "";
        //            cmd.Parameters.Add("@POR_IGV", SqlDbType.Decimal).Value = TAZA_IGV_100;
        //            cmd.Parameters.Add("@IMP_IVA", SqlDbType.Decimal).Value = 0.00m;
        //            cmd.Parameters.Add("@NUM_ITEM", SqlDbType.Char, 5).Value = nextItem.ToString("00000", CultureInfo.InvariantCulture);
        //            cmd.Parameters.Add("@IMP_PROD", SqlDbType.Char, 3).Value = Nz(imp);
        //            cmd.Parameters.Add("@SWT_IMPR", SqlDbType.Char, 1).Value = swtImpr;
        //            cmd.Parameters.Add("@PCT_CARG", SqlDbType.Decimal).Value = 0.00m;
        //            cmd.Parameters.Add("@IMP_CARG", SqlDbType.Decimal).Value = 0.00m;
        //            cmd.Parameters.Add("@ORI_PED", SqlDbType.Char, 8).Value = "";
        //            cmd.Parameters.Add("@CDG_COMB", SqlDbType.Char, 10).Value = combDb;
        //            cmd.Parameters.Add("@TDC_COMA", SqlDbType.Char, 3).Value = "";
        //            cmd.Parameters.Add("@DOC_COMA", SqlDbType.Char, 10).Value = "";

        //            cmd.Parameters["@CAN_PPRD"].Precision = 15; cmd.Parameters["@CAN_PPRD"].Scale = 4;
        //            cmd.Parameters["@PRE_PPRD"].Precision = 15; cmd.Parameters["@PRE_PPRD"].Scale = 4;
        //            cmd.Parameters["@IMP_TPRD"].Precision = 15; cmd.Parameters["@IMP_TPRD"].Scale = 2;
        //            cmd.Parameters["@PRE_IGV"].Precision = 15; cmd.Parameters["@PRE_IGV"].Scale = 4;
        //            cmd.Parameters["@IMP_IGV"].Precision = 15; cmd.Parameters["@IMP_IGV"].Scale = 2;

        //            cmd.ExecuteNonQuery();
        //        }
        //    }
        //}
        private static string ObtenerAmbienteDePedido(SqlConnection cn, SqlTransaction tx, string numPed8)
        {
            using (var c = new SqlCommand("SELECT ISNULL(LTRIM(RTRIM(CDG_AMB)),'001') FROM dbo.M_PEDIDO WHERE NUM_PED=@p;", cn, tx))
            {
                c.Parameters.Add("@p", SqlDbType.Char, 8).Value = To8(numPed8);
                var o = c.ExecuteScalar();
                var s = (o == null || o == DBNull.Value) ? "001" : Convert.ToString(o).Trim();
                return string.IsNullOrEmpty(s) ? "001" : s.PadLeft(3, '0');
            }
        }

        private static string MapAmbienteToListaPrecio(string cdgAmb) =>
            string.IsNullOrWhiteSpace(cdgAmb) ? "001" : cdgAmb.Trim().PadLeft(3, '0');

        private static (decimal valSinIgv, decimal preConIgv) GetPreciosDeLista(
            SqlConnection cn, SqlTransaction tx, string cdgLprc, string cdgProd10)
        {
            const string SQL = @"
            SELECT ISNULL(VAL_SOL,0) AS VAL_SOL, ISNULL(PRE_SOL,0) AS PRE_SOL
            FROM dbo.M_PRECIO
            WHERE CDG_LPRC = @l AND CDG_PROD = @p;";

            using (var cmd = new SqlCommand(SQL, cn, tx))
            {
                cmd.Parameters.Add("@l", SqlDbType.Char, 3).Value = cdgLprc;
                cmd.Parameters.Add("@p", SqlDbType.Char, 10).Value = cdgProd10;

                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        var val = rd["VAL_SOL"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["VAL_SOL"]);
                        var pre = rd["PRE_SOL"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["PRE_SOL"]);
                        return (val, pre);
                    }
                }
            }
            return (0m, 0m);
        }
        // === Método modificado ===
        private static void InsertarDetallesEnPedidoExistente(
            SqlConnection cn,
            SqlTransaction tx,
            string numPed8,
            IList<ceDPedido> nuevos,
            Func<string, string> resolverImpresora,
            Func<string, Tuple<decimal?, bool?>> resolverTrib)
        {
            const string SQL = @"
                INSERT INTO dbo.D_PEDIDO(
                    NUM_PED, CDG_PROD, CDG_FPRD, CAN_PPRD, PRE_PPRD, DCT_PPRD, DCT_FIC, IGV_PPRD, IMP_TPRD,
                    CAN_DPRD, CAN_FPRD, OBS_PPRD, CDG_LPRC, PRE_IGV, IMP_IGV,
                    CDG_PROM, FAC_UVTA, CDG_UVTA, COM_PPRD, CAN_PROD, CAN_OTRB, CAN_UVTA, PRE_UVTA, VAL_UVTA, TOT_UVTA,
                    POR_TISC, swt_igv, com_impo, SAC_PPRD, SWT_CMP, POR_IGV, IMP_IVA, NUM_ITEM, IMP_PROD, SWT_IMPR,
                    PCT_CARG, IMP_CARG, ORI_PED, CDG_COMB, TDC_COMA, DOC_COMA)
                VALUES(
                    @NUM_PED, @CDG_PROD, @CDG_FPRD, @CAN_PPRD, @PRE_PPRD, @DCT_PPRD, @DCT_FIC, @IGV_PPRD, @IMP_TPRD,
                    @CAN_DPRD, @CAN_FPRD, @OBS_PPRD, @CDG_LPRC, @PRE_IGV, @IMP_IGV,
                    @CDG_PROM, @FAC_UVTA, @CDG_UVTA, @COM_PPRD, @CAN_PROD, @CAN_OTRB, @CAN_UVTA, @PRE_UVTA, @VAL_UVTA, @TOT_UVTA,
                    @POR_TISC, @swt_igv, @com_impo, @SAC_PPRD, @SWT_CMP, @POR_IGV, @IMP_IVA, @NUM_ITEM, @IMP_PROD, @SWT_IMPR,
                    @PCT_CARG, @IMP_CARG, @ORI_PED, @CDG_COMB, @TDC_COMA, @DOC_COMA);";

            int nextItem = ObtenerMaxNumItem(cn, tx, numPed8);
            int maxComb = Math.Max(ObtenerMaxCdgComb(cn, tx, numPed8), 99);
            var mapComb = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Lista por defecto desde el AMBIENTE del pedido (p.ej. 001 -> 001)
            string amb = ObtenerAmbienteDePedido(cn, tx, numPed8);
            string listaPorAmbiente = MapAmbienteToListaPrecio(amb);

            using (var cmd = new SqlCommand(SQL, cn, tx))
            {
                foreach (var d in nuevos)
                {
                    nextItem++;

                    // --- Datos base ---
                    string cod10 = To10(d.COD10 ?? d.CDG_PROD.ToString(CultureInfo.InvariantCulture));
                    decimal can = Round4(d.CAN_PPRD);

                    // Lista de precios efectiva (la de la línea o la del ambiente)
                    string lprc = (d.CDG_LPRC > 0) ? d.CDG_LPRC.ToString("000") : listaPorAmbiente;

                    // Precios desde M_PRECIO (VAL_SOL sin IGV, PRE_SOL con IGV)
                    var (valSol, preSol) = GetPreciosDeLista(cn, tx, lprc, cod10);

                    // Unitarios
                    decimal pSinIgv = (valSol > 0m)
                                        ? Round4(valSol)
                                        : Round4((d.PRE_IGV > 0 ? d.PRE_IGV : d.PRE_PPRD * UNO_MAS_IGV) / UNO_MAS_IGV);

                    decimal pConIgv = (preSol > 0m)
                                        ? Round4(preSol)
                                        : Round4(d.PRE_IGV > 0 ? d.PRE_IGV : d.PRE_PPRD * UNO_MAS_IGV);

                    // Totales
                    decimal baseLinea = Round2(pSinIgv * can);                 // total SIN IGV
                    decimal igvLinea = Round2((pConIgv - pSinIgv) * can);     // IGV total
                    decimal impIgvCol = Round2(pConIgv);                       // IMP_IGV = PRE_IGV (2 dec.)

                    string notas = Nz(d.OBS_PPRD);

                    // swt_igv
                    string swtIgv;
                    bool hasSwt = false;
                    if (resolverTrib != null)
                    {
                        var t = resolverTrib(cod10);
                        if (t != null && t.Item2.HasValue) { hasSwt = true; swtIgv = t.Item2.Value ? "X" : ""; }
                        else swtIgv = "";
                    }
                    else swtIgv = "";
                    if (!hasSwt) swtIgv = GetSwtIgvFromProducto(cn, tx, cod10) ? "X" : "";

                    // Impresora
                    string imp = ObtenerImpresoraParaProducto(cn, tx, cod10, resolverImpresora);
                    string swtImpr = string.IsNullOrWhiteSpace(imp) ? "" : "X";

                    // Remap CDG_COMB local -> DB
                    string combInput = GetStrPropOrEmpty(d, "CDG_COMB");
                    string combDb = "";
                    if (!string.IsNullOrWhiteSpace(combInput))
                    {
                        if (!mapComb.TryGetValue(combInput, out int asign))
                        {
                            asign = ++maxComb;
                            mapComb[combInput] = asign;
                        }
                        combDb = To10(asign.ToString());
                    }

                    // --- Parámetros ---
                    cmd.Parameters.Clear();
                    cmd.Parameters.Add("@NUM_PED", SqlDbType.Char, 8).Value = To8(numPed8);
                    cmd.Parameters.Add("@CDG_PROD", SqlDbType.Char, 10).Value = cod10;
                    cmd.Parameters.Add("@CDG_FPRD", SqlDbType.Char, 3).Value = "000";
                    cmd.Parameters.Add("@CAN_PPRD", SqlDbType.Decimal).Value = can;

                    // *** REGLA: PRE_PPRD = VAL_SOL (sin IGV) ***
                    cmd.Parameters.Add("@PRE_PPRD", SqlDbType.Decimal).Value = pSinIgv;

                    cmd.Parameters.Add("@DCT_PPRD", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@DCT_FIC", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@IGV_PPRD", SqlDbType.Decimal).Value = 0.00m;

                    // Totales sin IGV
                    cmd.Parameters.Add("@IMP_TPRD", SqlDbType.Decimal).Value = baseLinea;

                    cmd.Parameters.Add("@CAN_DPRD", SqlDbType.Decimal).Value = 0.0000m;
                    cmd.Parameters.Add("@CAN_FPRD", SqlDbType.Decimal).Value = 0.0000m;
                    cmd.Parameters.Add("@OBS_PPRD", SqlDbType.Text).Value = notas;
                    cmd.Parameters.Add("@CDG_LPRC", SqlDbType.Char, 3).Value = lprc;

                    // *** PRE_IGV unitario y columnas por unidad ***
                    cmd.Parameters.Add("@PRE_IGV", SqlDbType.Decimal).Value = pConIgv;
                    // *** REGLA: IMP_IGV = PRE_IGV con 2 decimales ***
                    cmd.Parameters.Add("@IMP_IGV", SqlDbType.Decimal).Value = impIgvCol;

                    cmd.Parameters.Add("@CDG_PROM", SqlDbType.Char, 10).Value = "";
                    cmd.Parameters.Add("@FAC_UVTA", SqlDbType.Decimal).Value = Round10(1.0000000000m);
                    cmd.Parameters.Add("@CDG_UVTA", SqlDbType.Char, 3).Value = "001";
                    cmd.Parameters.Add("@COM_PPRD", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@CAN_PROD", SqlDbType.Decimal).Value = 0.0000m;
                    cmd.Parameters.Add("@CAN_OTRB", SqlDbType.Decimal).Value = 0.0000m;

                    cmd.Parameters.Add("@CAN_UVTA", SqlDbType.Decimal).Value = can;
                    cmd.Parameters.Add("@PRE_UVTA", SqlDbType.Decimal).Value = pConIgv;  // con IGV
                    cmd.Parameters.Add("@VAL_UVTA", SqlDbType.Decimal).Value = pSinIgv;  // sin IGV
                    cmd.Parameters.Add("@TOT_UVTA", SqlDbType.Decimal).Value = baseLinea; // sin IGV

                    cmd.Parameters.Add("@POR_TISC", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@swt_igv", SqlDbType.Char, 1).Value = swtIgv;
                    cmd.Parameters.Add("@com_impo", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@SAC_PPRD", SqlDbType.Char, 15).Value = "";
                    cmd.Parameters.Add("@SWT_CMP", SqlDbType.Char, 1).Value = "";
                    cmd.Parameters.Add("@POR_IGV", SqlDbType.Decimal).Value = TAZA_IGV_100;

                    // *** REGLA: IGV de la línea a IMP_IVA ***
                    cmd.Parameters.Add("@IMP_IVA", SqlDbType.Decimal).Value = igvLinea;

                    cmd.Parameters.Add("@NUM_ITEM", SqlDbType.Char, 5).Value = nextItem.ToString("00000", CultureInfo.InvariantCulture);
                    cmd.Parameters.Add("@IMP_PROD", SqlDbType.Char, 3).Value = Nz(imp);
                    cmd.Parameters.Add("@SWT_IMPR", SqlDbType.Char, 1).Value = swtImpr;

                    cmd.Parameters.Add("@PCT_CARG", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@IMP_CARG", SqlDbType.Decimal).Value = 0.00m;
                    cmd.Parameters.Add("@ORI_PED", SqlDbType.Char, 8).Value = "";
                    cmd.Parameters.Add("@CDG_COMB", SqlDbType.Char, 10).Value = combDb;
                    cmd.Parameters.Add("@TDC_COMA", SqlDbType.Char, 3).Value = "";
                    cmd.Parameters.Add("@DOC_COMA", SqlDbType.Char, 10).Value = "";

                    // Precisión / escala
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
            decimal st = 0m, igv = 0m;
            using (var cmd = new SqlCommand(@"
                SELECT ISNULL(SUM(IMP_TPRD),0) AS ST, ISNULL(SUM(IMP_IGV),0) AS IGV
                FROM dbo.D_PEDIDO WHERE NUM_PED=@P;", cn, tx))
            {
                cmd.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);
                using (var rd = cmd.ExecuteReader()) { if (rd.Read()) { st = rd["ST"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["ST"]); igv = rd["IGV"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IGV"]); } }
            }
            decimal impStot = Round2(st);
            decimal impTigv = Round2(igv);
            decimal impTtot = Round2(impStot + impTigv);

            using (var u = new SqlCommand(@"UPDATE dbo.M_PEDIDO SET IMP_STOT=@ST, IMP_TIGV=@IGV, IMP_TTOT=@TT WHERE NUM_PED=@P;", cn, tx))
            {
                u.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);
                u.Parameters.Add("@ST", SqlDbType.Decimal).Value = impStot;
                u.Parameters.Add("@IGV", SqlDbType.Decimal).Value = impTigv;
                u.Parameters.Add("@TT", SqlDbType.Decimal).Value = impTtot;
                u.Parameters["@ST"].Precision = 15; u.Parameters["@ST"].Scale = 2;
                u.Parameters["@IGV"].Precision = 15; u.Parameters["@IGV"].Scale = 2;
                u.Parameters["@TT"].Precision = 15; u.Parameters["@TT"].Scale = 2;
                u.ExecuteNonQuery();
            }
        }

        // ======== Consultas ========
        public string ObtenerNumPedAbiertoPorMesa(string cdgMesa)
        {
            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var cmd = new SqlCommand(@"
                SELECT TOP 1 m.NUM_PED
                FROM dbo.M_PEDIDO m
                WHERE (
                        m.NUM_MESA = @MESA
                        OR RIGHT('00' + m.NUM_MESA, 3) = RIGHT('00' + @MESA, 3)
                        OR RIGHT('00' + @MESA, 3) = RIGHT('00' + m.NUM_MESA, 3)
                      )
                  AND (m.SWT_PED IS NULL OR LTRIM(RTRIM(m.SWT_PED)) = '')
                ORDER BY m.FEC_PED DESC;", cn))
                {
                    cmd.Parameters.Add("@MESA", SqlDbType.VarChar, 3).Value = (cdgMesa ?? "").Trim().PadLeft(3, '0');
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
                using (var cmd = new SqlCommand("SELECT CASE WHEN (SWT_PED IS NULL OR LTRIM(RTRIM(SWT_PED))='') THEN 1 ELSE 0 END FROM dbo.M_PEDIDO WHERE NUM_PED=@P;", cn))
                { cmd.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8); return Convert.ToInt32(cmd.ExecuteScalar() ?? 0) == 1; }
            }
        }
        public ceMPedido ObtenerCabeceraPorNum(string numPed8)
        {
            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var cmd = new SqlCommand(@"
                SELECT NUM_PED, NUM_MESA, SWT_PED, FEC_PED, CDG_VEND, CDG_AMB, NUM_PERS, IMP_STOT, IMP_TIGV, IMP_TTOT
                FROM dbo.M_PEDIDO WHERE NUM_PED=@P;", cn))
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
                            IMP_BASE = rd["IMP_STOT"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IMP_STOT"])
                        };
                    }
                }
            }
        }
        public List<ceDPedido> ObtenerDetallePorPedido(string numPed8)
        {
            var list = new List<ceDPedido>();
            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var cmd = new SqlCommand(@"
                SELECT d.NUM_PED, d.CDG_FPRD, d.NUM_ITEM, d.CDG_COMB, d.CDG_PROD,
                       d.CAN_PPRD, d.PRE_PPRD, d.IMP_TPRD, d.PRE_IGV, d.IMP_IGV,
                       d.OBS_PPRD, d.CDG_LPRC, d.IMP_PROD, d.SWT_IMPR,
                       p.DES_PROD AS DESCRIPCION
                FROM dbo.D_PEDIDO d
                LEFT JOIN dbo.M_PRODUC p ON p.CDG_PROD = d.CDG_PROD
                WHERE d.NUM_PED=@P
                ORDER BY d.NUM_ITEM;", cn))
                {
                    cmd.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            string cdgProdStr = Convert.ToString(rd["CDG_PROD"]).Trim();
                            int cdgProdInt; int.TryParse(cdgProdStr, out cdgProdInt);
                            string cod10 = (cdgProdInt > 0 ? cdgProdInt.ToString() : cdgProdStr).PadLeft(10, '0');

                            var d = new ceDPedido
                            {
                                CDG_PROD = cdgProdInt,
                                COD10 = cod10,
                                CAN_PPRD = rd["CAN_PPRD"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["CAN_PPRD"]),
                                PRE_PPRD = rd["PRE_PPRD"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["PRE_PPRD"]),
                                IMP_TPRD = rd["IMP_TPRD"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IMP_TPRD"]),
                                PRE_IGV = rd["PRE_IGV"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["PRE_IGV"]),
                                IMP_IGV = rd["IMP_IGV"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IMP_IGV"]),
                                OBS_PPRD = rd["OBS_PPRD"] == DBNull.Value ? "" : Convert.ToString(rd["OBS_PPRD"]).Trim(),
                                CDG_LPRC = rd["CDG_LPRC"] == DBNull.Value ? 0 : Convert.ToInt32(rd["CDG_LPRC"]),
                                IMP_PROD = rd["IMP_PROD"] == DBNull.Value ? "" : Convert.ToString(rd["IMP_PROD"]).Trim(),
                                SWT_IMPR = rd["SWT_IMPR"] == DBNull.Value ? (bool?)null : (Convert.ToString(rd["SWT_IMPR"]).Trim().Equals("X", StringComparison.OrdinalIgnoreCase) ? true : (bool?)false),
                                DESCRIPCION = rd["DESCRIPCION"] == DBNull.Value ? "" : Convert.ToString(rd["DESCRIPCION"]).Trim()
                            };
                            if (rd["CDG_FPRD"] != DBNull.Value) d.CDG_FPRD = Convert.ToInt32(rd["CDG_FPRD"]);
                            string numItemDb = rd["NUM_ITEM"] == DBNull.Value ? "" : Convert.ToString(rd["NUM_ITEM"]).Trim();
                            if (!string.IsNullOrEmpty(numItemDb)) SetPropIfExists(d, "NUM_ITEM", IsDigits(numItemDb) ? numItemDb.PadLeft(5, '0') : numItemDb);
                            string combDb = rd["CDG_COMB"] == DBNull.Value ? "" : Convert.ToString(rd["CDG_COMB"]).Trim();
                            if (!string.IsNullOrEmpty(combDb)) SetPropIfExists(d, "CDG_COMB", IsDigits(combDb) ? Convert.ToInt32(combDb).ToString() : combDb);
                            list.Add(d);
                        }
                    }
                }
            }
            return list;
        }

        // ======== Eliminación + recálculo ========
        public int EliminarDetallesSeleccion(string numPed8, string cdgComb, string numItem)
        {
            if (string.IsNullOrWhiteSpace(numPed8)) throw new ArgumentException("NUM_PED vacío.", nameof(numPed8));
            int filas = 0;
            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(cdgComb))
                        {
                            using (var cmd = new SqlCommand("DELETE FROM dbo.D_PEDIDO WHERE NUM_PED=@n AND CDG_COMB=@c;", cn, tx))
                            { cmd.Parameters.Add("@n", SqlDbType.Char, 8).Value = To8(numPed8); cmd.Parameters.Add("@c", SqlDbType.Char, 10).Value = To10(cdgComb); filas += cmd.ExecuteNonQuery(); }
                        }
                        if (!string.IsNullOrWhiteSpace(numItem))
                        {
                            using (var cmd = new SqlCommand("DELETE FROM dbo.D_PEDIDO WHERE NUM_PED=@n AND NUM_ITEM=@i;", cn, tx))
                            { cmd.Parameters.Add("@n", SqlDbType.Char, 8).Value = To8(numPed8); cmd.Parameters.Add("@i", SqlDbType.Char, 5).Value = numItem.Trim().PadLeft(5, '0'); filas += cmd.ExecuteNonQuery(); }
                        }
                        RecalcularTotalesCabeceraDesdeDetalle(cn, tx, numPed8);
                        tx.Commit();
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
            return filas;
        }
        public int EliminarDetallePorNumItem(string numPed8, string numItem5) => EliminarDetallesSeleccion(numPed8, null, numItem5);
        public int EliminarDetallePorCombo(string numPed8, string cdgComb) => EliminarDetallesSeleccion(numPed8, cdgComb, null);

        // Compatibilidad: por CDG_FPRD
        public int EliminarDetalle(string numPed, int cdgFprd)
        {
            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        int filas;
                        using (var cmd = new SqlCommand("DELETE FROM dbo.D_PEDIDO WHERE NUM_PED=@n AND CDG_FPRD=@f;", cn, tx))
                        { cmd.Parameters.Add("@n", SqlDbType.Char, 8).Value = To8(numPed); cmd.Parameters.Add("@f", SqlDbType.Int).Value = cdgFprd; filas = cmd.ExecuteNonQuery(); }
                        RecalcularTotalesCabeceraDesdeDetalle(cn, tx, To8(numPed));
                        tx.Commit();
                        return filas;
                    }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        // Recalcular opcion rápida (a partir de PRE_IGV)
        public void RecalcularTotales(string numPed)
        {
            const string SQL = @"
            ;WITH x AS (
                SELECT ISNULL(SUM(PRE_IGV * CAN_PPRD),0) AS TotConIgv
                FROM dbo.D_PEDIDO WHERE NUM_PED=@n
            )
            UPDATE dbo.M_PEDIDO
               SET IMP_STOT = ROUND(x.TotConIgv / (1 + @pIgv), 2),
                   IMP_TIGV = ROUND(x.TotConIgv - (x.TotConIgv / (1 + @pIgv)), 2),
                   IMP_TTOT = ROUND(x.TotConIgv, 2)
              FROM x WHERE NUM_PED=@n;";
            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(SQL, cn))
            {
                cmd.Parameters.AddWithValue("@n", (numPed ?? "").Trim());
                cmd.Parameters.Add("@pIgv", SqlDbType.Decimal).Value = TAZA_IGV_FRAC;
                cmd.Parameters["@pIgv"].Precision = 5; cmd.Parameters["@pIgv"].Scale = 2;
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
