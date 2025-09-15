using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using CapaEntidad;

namespace CapaDatos
{
    public class DAOPedido
    {
        private readonly string _cs;

        public DAOPedido()
        {
            _cs = Conexion.Cadena;
        }

        // ================= Helpers =================

        private static string To8(string cod)
        {
            string s = (cod ?? "").Trim();
            if (s.Length == 0) return "00000000";
            if (IsDigits(s)) return s.PadLeft(8, '0');

            // limpia no dígitos
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

        private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal Round4(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);
        private static decimal Round10(decimal v) => Math.Round(v, 10, MidpointRounding.AwayFromZero);

        private static string Nz(string s) => s == null ? "" : s.Trim();

        private static bool GetSwtIgvFromProducto(SqlConnection cn, SqlTransaction tx, string cod10)
        {
            using (var cmd = new SqlCommand(
                "SELECT SWT_IGV FROM dbo.M_PRODUC WHERE CDG_PROD = @p", cn, tx))
            {
                cmd.Parameters.Add("@p", SqlDbType.Char, 10).Value = To10(cod10);
                object o = cmd.ExecuteScalar();

                if (o == null || o == DBNull.Value) return false;
                var s = Convert.ToString(o).Trim();

                // Acepta varias representaciones (numérico/char)
                return s == "1"
                       || s.Equals("X", StringComparison.OrdinalIgnoreCase)
                       || s.Equals("S", StringComparison.OrdinalIgnoreCase)
                       || s.Equals("SI", StringComparison.OrdinalIgnoreCase);
            }
        }

        // Siguiente correlativo NUM_PED (8 dígitos)
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

        // ================= Público =================

        /// <summary>
        /// Inserta cabecera y detalle. Devuelve NUM_PED generado (8 dígitos).
        /// resolverImpresora: recibe COD10 y devuelve "003" o "".
        /// resolverTrib: opcional; recibe COD10 y devuelve Tuple(porIgvDecimal, swtIgvBool).
        /// </summary>
        public string InsertarPedido(
            ceMPedido cab,
            Func<string, string> resolverImpresora,
            Func<string, Tuple<decimal?, bool?>> resolverTrib // puede ser null
        )
        {
            if (cab == null) throw new ArgumentNullException(nameof(cab));
            if (cab.Detalles == null || cab.Detalles.Count == 0)
                throw new InvalidOperationException("El pedido no contiene detalles.");

            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var tx = cn.BeginTransaction())
                {
                    try
                    {
                        // === NUM_PED correlativo (8 dígitos) ===
                        string numPed = ObtenerSiguienteNumPed(cn, tx);
                        cab.NUM_PED = numPed;

                        // === M_PEDIDO ===
                        InsertarCabecera(cn, tx, cab);

                        // === D_PEDIDO ===
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
            // ====== Constantes / defaults ======
            const string CDG_CPAG = "000";
            const string CDG_MON_DEF = "001"; // fijo
            const string SWT_PTV_FIJO = "S";  // fijo
            const string ORI_AREA = "001";
            const string CDG_PRIO = "000";
            const string CDG_LOC_DEF = "000";

            const decimal POR_TIGV_FIJO = 10.00m; // 10.00
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

            // ====== Cálculos ======
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
                cmd.Parameters.Add("@CDG_MON", SqlDbType.Char, 3).Value = CDG_MON_DEF; // '001'
                cmd.Parameters.Add("@FEC_PED", SqlDbType.SmallDateTime).Value = cab.FEC_PED;

                cmd.Parameters.Add("@NUM_OCOM", SqlDbType.Char, 60).Value = "";
                cmd.Parameters.Add("@IMP_STOT", SqlDbType.Decimal).Value = impStot;
                cmd.Parameters.Add("@IMP_TIGV", SqlDbType.Decimal).Value = impTigv;
                cmd.Parameters.Add("@IMP_TDCT", SqlDbType.Decimal).Value = IMP_TDCT_FIJO;
                cmd.Parameters.Add("@IMP_TTOT", SqlDbType.Decimal).Value = impTtot;
                cmd.Parameters.Add("@POR_TDCT", SqlDbType.Decimal).Value = POR_TDCT_FIJO;
                cmd.Parameters.Add("@POR_TIGV", SqlDbType.Decimal).Value = POR_TIGV_FIJO;

                cmd.Parameters.Add("@OBS_PED", SqlDbType.Text).Value = Nz(cab.OBS_PED);
                cmd.Parameters.Add("@SWT_PED", SqlDbType.Char, 1).Value = ""; // abierto: vacío
                cmd.Parameters.Add("@RUC_CLI", SqlDbType.Char, 8).Value = "00000000";
                cmd.Parameters.Add("@SWT_COT", SqlDbType.Decimal).Value = 0;
                cmd.Parameters.Add("@SWT_PTV", SqlDbType.Char, 1).Value = SWT_PTV_FIJO; // 'S'
                cmd.Parameters.Add("@ORI_AREA", SqlDbType.Char, 3).Value = ORI_AREA;    // '001'
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

                // Precisiones
                cmd.Parameters["@IMP_STOT"].Precision = 15; cmd.Parameters["@IMP_STOT"].Scale = 2;
                cmd.Parameters["@IMP_TIGV"].Precision = 15; cmd.Parameters["@IMP_TIGV"].Scale = 2;
                cmd.Parameters["@IMP_TDCT"].Precision = 15; cmd.Parameters["@IMP_TDCT"].Scale = 2;
                cmd.Parameters["@IMP_TTOT"].Precision = 15; cmd.Parameters["@IMP_TTOT"].Scale = 2;
                cmd.Parameters["@POR_TDCT"].Precision = 7; cmd.Parameters["@POR_TDCT"].Scale = 2;
                cmd.Parameters["@POR_TIGV"].Precision = 7; cmd.Parameters["@POR_TIGV"].Scale = 2;
                cmd.Parameters["@NUM_PERS"].Precision = 3; cmd.Parameters["@NUM_PERS"].Scale = 0;
                cmd.Parameters["@VAL_DPVT"].Precision = 15; cmd.Parameters["@VAL_DPVT"].Scale = 2;
                cmd.Parameters["@POR_DPVT"].Precision = 7; cmd.Parameters["@POR_DPVT"].Scale = 2;
                cmd.Parameters["@POR_IVA"].Precision = 7; cmd.Parameters["@POR_IVA"].Scale = 2;
                cmd.Parameters["@POR_ICA"].Precision = 7; cmd.Parameters["@POR_ICA"].Scale = 2;
                cmd.Parameters["@POR_FTE"].Precision = 7; cmd.Parameters["@POR_FTE"].Scale = 2;
                cmd.Parameters["@VAL_CARG"].Precision = 15; cmd.Parameters["@VAL_CARG"].Scale = 2;
                cmd.Parameters["@POR_CARG"].Precision = 7; cmd.Parameters["@POR_CARG"].Scale = 2;
                cmd.Parameters["@IMP_TISC"].Precision = 15; cmd.Parameters["@IMP_TISC"].Scale = 2;

                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertarDetalles(
            SqlConnection cn, SqlTransaction tx, ceMPedido cab,
            Func<string, string> resolverImpresora,
            Func<string, Tuple<decimal?, bool?>> resolverTrib)
        {
            // ===== Defaults/constantes para D_PEDIDO =====
            const string CDG_FPRD = "000";
            const string CDG_PROM = "";
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
            const string CDG_COMB = "";  // en blanco
            const string TDC_COMA = "";
            const string DOC_COMA = "";

            const decimal POR_IGV_100 = 10.00m; // porcentaje 10.00

            // ===== PRE-CHECK: todos los códigos deben existir en M_PRODUC =====
            var cods = new HashSet<string>();
            for (int i = 0; i < cab.Detalles.Count; i++)
            {
                var dd = cab.Detalles[i];
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
                throw new InvalidOperationException(
                    "Los siguientes CDG_PROD no existen en M_PRODUC: " + string.Join(", ", faltantes));

            // ===== Insert =====
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

                for (int i = 0; i < cab.Detalles.Count; i++)
                {
                    var d = cab.Detalles[i];
                    item++;

                    // --- Datos base del detalle ---
                    string cod10 = To10(d.COD10 ?? d.CDG_PROD.ToString(CultureInfo.InvariantCulture));
                    decimal can = Round4(d.CAN_PPRD);   // 1.0000
                    decimal pre = Round4(d.PRE_PPRD);   // 20.0000 (sin IGV)
                    decimal impt = Round2(d.IMP_TPRD);  // 20.00   (total sin IGV)
                    decimal preI = Round4(d.PRE_IGV);   // 22.0000 (con IGV)
                    decimal igvI = Round2(d.IMP_IGV);   // 22.00   (total con IGV)

                    string notas = Nz(d.OBS_PPRD);
                    string lprc = (d.CDG_LPRC <= 0) ? "001" : d.CDG_LPRC.ToString("000");

                    // --- UVTA e IVA (para coincidir con la fila “correcta”) ---
                    decimal preUvta = preI;  // 22.0000 (unitario con IGV)
                    decimal valUvta = pre;   // 20.0000 (unitario sin IGV)
                    decimal totUvta = impt;  // 20.00   (total sin IGV)
                    decimal impIva = Math.Max(0m, Math.Round(igvI - impt, 2)); // 2.00

                    // --- swt_igv: resolverTrib o maestro ---
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

                    // --- Impresora/flag ---
                    string imp = (resolverImpresora != null) ? Nz(resolverImpresora(cod10)) : "";
                    string swtImpr = (imp.Length == 0) ? "" : "X";

                    cmd.Parameters.Clear();

                    // columnas base
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

                    // uvta y varios
                    cmd.Parameters.Add("@CDG_PROM", SqlDbType.Char, 10).Value = CDG_PROM;
                    cmd.Parameters.Add("@FAC_UVTA", SqlDbType.Decimal).Value = Round10(FAC_UVTA);
                    cmd.Parameters.Add("@CDG_UVTA", SqlDbType.Char, 3).Value = CDG_UVTA_DEF;
                    cmd.Parameters.Add("@COM_PPRD", SqlDbType.Decimal).Value = COM_PPRD;
                    cmd.Parameters.Add("@CAN_PROD", SqlDbType.Decimal).Value = CAN_PROD;
                    cmd.Parameters.Add("@CAN_OTRB", SqlDbType.Decimal).Value = CAN_OTRB;

                    cmd.Parameters.Add("@CAN_UVTA", SqlDbType.Decimal).Value = can;
                    cmd.Parameters.Add("@PRE_UVTA", SqlDbType.Decimal).Value = preUvta; // 22.0000
                    cmd.Parameters.Add("@VAL_UVTA", SqlDbType.Decimal).Value = valUvta; // 20.0000
                    cmd.Parameters.Add("@TOT_UVTA", SqlDbType.Decimal).Value = totUvta; // 20.00

                    cmd.Parameters.Add("@POR_TISC", SqlDbType.Decimal).Value = POR_TISC;
                    cmd.Parameters.Add("@swt_igv", SqlDbType.Char, 1).Value = swt_igv_text;

                    // numérico: evitar NULL -> 0.00
                    cmd.Parameters.Add("@com_impo", SqlDbType.Decimal).Value = 0.00m;

                    cmd.Parameters.Add("@SAC_PPRD", SqlDbType.Char, 15).Value = "";
                    cmd.Parameters.Add("@SWT_CMP", SqlDbType.Char, 1).Value = "";

                    // IGV % y IVA calculado
                    cmd.Parameters.Add("@POR_IGV", SqlDbType.Decimal).Value = POR_IGV_100; // 10.00
                    cmd.Parameters.Add("@IMP_IVA", SqlDbType.Decimal).Value = impIva;      // 2.00

                    // NUM_ITEM con 5 dígitos
                    cmd.Parameters.Add("@NUM_ITEM", SqlDbType.Char, 5)
                       .Value = item.ToString("00000", CultureInfo.InvariantCulture);

                    cmd.Parameters.Add("@IMP_PROD", SqlDbType.Char, 3).Value = Nz(imp);
                    cmd.Parameters.Add("@SWT_IMPR", SqlDbType.Char, 1).Value = swtImpr;

                    cmd.Parameters.Add("@PCT_CARG", SqlDbType.Decimal).Value = PCT_CARG;
                    cmd.Parameters.Add("@IMP_CARG", SqlDbType.Decimal).Value = IMP_CARG;
                    cmd.Parameters.Add("@ORI_PED", SqlDbType.Char, 8).Value = ORI_PED;

                    // vacíos
                    cmd.Parameters.Add("@CDG_COMB", SqlDbType.Char, 10).Value = CDG_COMB;
                    cmd.Parameters.Add("@TDC_COMA", SqlDbType.Char, 3).Value = TDC_COMA;
                    cmd.Parameters.Add("@DOC_COMA", SqlDbType.Char, 10).Value = DOC_COMA;

                    // Precisiones/escala
                    cmd.Parameters["@CAN_PPRD"].Precision = 15; cmd.Parameters["@CAN_PPRD"].Scale = 4;
                    cmd.Parameters["@PRE_PPRD"].Precision = 15; cmd.Parameters["@PRE_PPRD"].Scale = 4;
                    cmd.Parameters["@DCT_PPRD"].Precision = 7; cmd.Parameters["@DCT_PPRD"].Scale = 2;
                    cmd.Parameters["@DCT_FIC"].Precision = 7; cmd.Parameters["@DCT_FIC"].Scale = 2;
                    cmd.Parameters["@IGV_PPRD"].Precision = 7; cmd.Parameters["@IGV_PPRD"].Scale = 2;
                    cmd.Parameters["@IMP_TPRD"].Precision = 15; cmd.Parameters["@IMP_TPRD"].Scale = 2;

                    cmd.Parameters["@CAN_DPRD"].Precision = 15; cmd.Parameters["@CAN_DPRD"].Scale = 4;
                    cmd.Parameters["@CAN_FPRD"].Precision = 15; cmd.Parameters["@CAN_FPRD"].Scale = 4;
                    cmd.Parameters["@PRE_IGV"].Precision = 15; cmd.Parameters["@PRE_IGV"].Scale = 4;
                    cmd.Parameters["@IMP_IGV"].Precision = 15; cmd.Parameters["@IMP_IGV"].Scale = 2;

                    cmd.Parameters["@FAC_UVTA"].Precision = 15; cmd.Parameters["@FAC_UVTA"].Scale = 10;
                    cmd.Parameters["@COM_PPRD"].Precision = 7; cmd.Parameters["@COM_PPRD"].Scale = 2;
                    cmd.Parameters["@CAN_PROD"].Precision = 15; cmd.Parameters["@CAN_PROD"].Scale = 4;
                    cmd.Parameters["@CAN_OTRB"].Precision = 15; cmd.Parameters["@CAN_OTRB"].Scale = 4;

                    cmd.Parameters["@CAN_UVTA"].Precision = 15; cmd.Parameters["@CAN_UVTA"].Scale = 4;
                    cmd.Parameters["@PRE_UVTA"].Precision = 15; cmd.Parameters["@PRE_UVTA"].Scale = 4;
                    cmd.Parameters["@VAL_UVTA"].Precision = 15; cmd.Parameters["@VAL_UVTA"].Scale = 4;
                    cmd.Parameters["@TOT_UVTA"].Precision = 15; cmd.Parameters["@TOT_UVTA"].Scale = 2;

                    cmd.Parameters["@POR_TISC"].Precision = 7; cmd.Parameters["@POR_TISC"].Scale = 2;

                    cmd.Parameters["@com_impo"].Precision = 15; cmd.Parameters["@com_impo"].Scale = 2;
                    cmd.Parameters["@POR_IGV"].Precision = 7; cmd.Parameters["@POR_IGV"].Scale = 2;
                    cmd.Parameters["@IMP_IVA"].Precision = 15; cmd.Parameters["@IMP_IVA"].Scale = 2;

                    cmd.Parameters["@PCT_CARG"].Precision = 7; cmd.Parameters["@PCT_CARG"].Scale = 2;
                    cmd.Parameters["@IMP_CARG"].Precision = 15; cmd.Parameters["@IMP_CARG"].Scale = 2;

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ================= Consultas para Mesas / Estado =================

        /// <summary>
        /// Devuelve el NUM_PED (8 dígitos) del pedido ABIERTO de una mesa (SWT_PED '' o NULL).
        /// Si no hay, devuelve null.
        /// </summary>
        public string ObtenerNumPedAbiertoPorMesa(string cdgMesa)
        {
            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var cmd = new SqlCommand(@"
            SELECT TOP 1 NUM_PED
            FROM dbo.M_PEDIDO
            WHERE NUM_MESA = @MESA               -- <<< ANTES: CDG_MESA
              AND (SWT_PED IS NULL OR LTRIM(RTRIM(SWT_PED)) = '')
            ORDER BY FEC_PED DESC;", cn))
                {
                    cmd.Parameters.Add("@MESA", SqlDbType.Char, 3).Value = (cdgMesa ?? "").PadLeft(3, '0');
                    object o = cmd.ExecuteScalar();
                    return (o == null || o == DBNull.Value) ? null : Convert.ToString(o);
                }
            }
        }

        /// <summary>
        /// ¿El NUM_PED sigue abierto? (SWT_PED '' o NULL)
        /// </summary>
        public bool PedidoSigueAbierto(string numPed8)
        {
            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var cmd = new SqlCommand(@"
                SELECT CASE WHEN (SWT_PED IS NULL OR SWT_PED = '') THEN 1 ELSE 0 END
                FROM dbo.M_PEDIDO
                WHERE NUM_PED = @P;", cn))
                {
                    cmd.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);
                    var r = cmd.ExecuteScalar();
                    return r != null && Convert.ToInt32(r) == 1;
                }
            }
        }

        /// <summary>
        /// (Opcional) Trae cabecera básica por NUM_PED (para mostrar info en la UI).
        /// </summary>
        public ceMPedido ObtenerCabeceraPorNum(string numPed8)
        {
            using (var cn = new SqlConnection(_cs))
            {
                cn.Open();
                using (var cmd = new SqlCommand(@"
            SELECT 
                NUM_PED,
                NUM_MESA AS CDG_MESA,    -- <== la columna real en BD es NUM_MESA
                SWT_PED,
                FEC_PED,
                CDG_VEND,
                IMP_STOT,
                IMP_TIGV,
                IMP_TTOT
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
                            CDG_MESA = Convert.ToString(rd["CDG_MESA"]).Trim(),     // viene del alias a NUM_MESA
                            SWT_PED = Convert.ToString(rd["SWT_PED"]).Trim(),
                            FEC_PED = rd["FEC_PED"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(rd["FEC_PED"]),
                            CDG_VEND = Convert.ToString(rd["CDG_VEND"]).Trim(),
                            IMP_BASE = rd["IMP_STOT"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IMP_STOT"]),
                            // Si luego necesitas, puedes mapear IMP_IGV / IMP_TOT:
                            // IMP_IGV = rd["IMP_TIGV"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IMP_TIGV"]),
                            // IMP_TOT = rd["IMP_TTOT"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IMP_TTOT"]),
                        };
                    }
                }
            }
        }


        /// <summary>
        /// Devuelve la lista de detalles (D_PEDIDO) de un NUM_PED para reingresar a la mesa.
        /// </summary>
        //public List<ceDPedido> ObtenerDetallePorPedido(string numPed8)
        //{
        //    var lista = new List<ceDPedido>();

        //    using (var cn = new SqlConnection(_cs))
        //    {
        //        cn.Open();
        //        using (var cmd = new SqlCommand(@"
        //        SELECT 
        //            NUM_PED,
        //            CDG_PROD,         -- char(10) en BD
        //            CAN_PPRD, PRE_PPRD, IMP_TPRD,
        //            PRE_IGV, IMP_IGV,
        //            OBS_PPRD,
        //            CDG_LPRC          -- numérico (consulta cómo está en tu BD)
        //        FROM dbo.D_PEDIDO
        //        WHERE NUM_PED = @P
        //        ORDER BY NUM_ITEM;", cn))
        //        {
        //            cmd.Parameters.Add("@P", SqlDbType.Char, 8).Value = To8(numPed8);

        //            using (var rd = cmd.ExecuteReader())
        //            {
        //                while (rd.Read())
        //                {
        //                    // CDG_PROD viene como char(10) en BD; tu entidad puede tener:
        //                    //  a) string COD10, o
        //                    //  b) int CDG_PROD (como usas en InsertarDetalles con d.CDG_PROD.ToString()).
        //                    // Te dejo ambos sets: deja A si tienes COD10(string); deja B si tienes CDG_PROD(int).

        //                    // ---- A) Si tu entidad tiene COD10 (string) ----
        //                    string cod10 = (rd["CDG_PROD"] == DBNull.Value) ? "" : Convert.ToString(rd["CDG_PROD"]).Trim();

        //                    // ---- B) Si tu entidad tiene CDG_PROD (int) ----
        //                    int cdgProdInt = 0;
        //                    if (!string.IsNullOrEmpty(cod10))
        //                        int.TryParse(cod10, out cdgProdInt);

        //                    var det = new ceDPedido
        //                    {
        //                        // Descomenta una de estas dos líneas según tu entidad:
        //                        // COD10 = cod10,                 // <- si tienes propiedad COD10 (string)
        //                        CDG_PROD = cdgProdInt,           // <- si tu propiedad es CDG_PROD (int)

        //                        CAN_PPRD = rd["CAN_PPRD"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["CAN_PPRD"]),
        //                        PRE_PPRD = rd["PRE_PPRD"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["PRE_PPRD"]),
        //                        IMP_TPRD = rd["IMP_TPRD"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IMP_TPRD"]),
        //                        PRE_IGV = rd["PRE_IGV"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["PRE_IGV"]),
        //                        IMP_IGV = rd["IMP_IGV"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IMP_IGV"]),
        //                        OBS_PPRD = rd["OBS_PPRD"] == DBNull.Value ? "" : Convert.ToString(rd["OBS_PPRD"]).Trim(),
        //                        CDG_LPRC = rd["CDG_LPRC"] == DBNull.Value ? 0 : Convert.ToInt32(rd["CDG_LPRC"]),
        //                    };

        //                    lista.Add(det);
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
                d.CDG_PROD,         -- char(10)
                d.CAN_PPRD, d.PRE_PPRD, d.IMP_TPRD,
                d.PRE_IGV, d.IMP_IGV,
                d.OBS_PPRD,
                d.CDG_LPRC,
                d.NUM_ITEM,
                d.IMP_PROD,
                d.SWT_IMPR,
                p.DES_PROD AS DESCRIPCION   -- <-- usa el nombre real de tu columna
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
                            // CDG_PROD viene como CHAR(10) -> si quieres int, parsea; si no, usa COD10
                            string cod10 = rd["CDG_PROD"] == DBNull.Value ? "" : Convert.ToString(rd["CDG_PROD"]).Trim();
                            int cdgProdInt = 0; int.TryParse(cod10, out cdgProdInt);

                            var d = new ceDPedido
                            {
                                // elige una de estas dos según cómo uses la entidad en tu UI
                                COD10 = cod10,
                                CDG_PROD = cdgProdInt,

                                CAN_PPRD = rd["CAN_PPRD"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["CAN_PPRD"]),
                                PRE_PPRD = rd["PRE_PPRD"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["PRE_PPRD"]),
                                IMP_TPRD = rd["IMP_TPRD"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IMP_TPRD"]),
                                PRE_IGV = rd["PRE_IGV"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["PRE_IGV"]),
                                IMP_IGV = rd["IMP_IGV"] == DBNull.Value ? 0m : Convert.ToDecimal(rd["IMP_IGV"]),
                                OBS_PPRD = rd["OBS_PPRD"] == DBNull.Value ? "" : Convert.ToString(rd["OBS_PPRD"]).Trim(),
                                CDG_LPRC = rd["CDG_LPRC"] == DBNull.Value ? 0 : Convert.ToInt32(rd["CDG_LPRC"]),
                                IMP_PROD = rd["IMP_PROD"] == DBNull.Value ? "" : Convert.ToString(rd["IMP_PROD"]).Trim(),
                                SWT_IMPR = rd["SWT_IMPR"] == DBNull.Value ? (bool?)null :
                                            (Convert.ToString(rd["SWT_IMPR"]).Trim().Equals("X", StringComparison.OrdinalIgnoreCase) ? true : (bool?)false),

                                // aquí llega el nombre del producto desde M_PRODUC
                                DESCRIPCION = rd["DESCRIPCION"] == DBNull.Value ? "" : Convert.ToString(rd["DESCRIPCION"]).Trim()
                            };

                            lista.Add(d);
                        }
                    }
                }
            }
            return lista;
        }
    }
}
