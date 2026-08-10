# Artifact Triage

- Path: `D:\unity-mcp-beta\DWR-932_fw_revb_202eu_ALL_multi_20150119\DWR-932_B1_02.02EU\2K-mdm9625-usr-image.usrfs.yaffs2`
- Size: 27439104 bytes
- SHA-256: `b46196f4c58a145bb1fb53a7fde8516298694997188be1f631674d2ee62fa0d1`
- Extension hint: -
- Magic hints: unknown
- Prefix entropy: 6.008
- Suggested profiles: malware-triage
- Generated UTC: 2026-08-10T01:31:38.242345+00:00

## Recommended local tools
- capa
- floss
- yara
- sandbox trace if authorized

## Recommended next steps
- Run capability detection and validate suspicious terms with xrefs

## Indicators
### suspicious_terms
- `Crypt`
- `OpenSSL`
- `connect`
- `socket`

## Sample strings

- `libssl.so.1.0.0`
- `/lib/ld-linux.so.3`
- `l[)AL`
- `G)A8`
- `p>)A `
- `4')Ap`
- `8f'A`
- `a)A<`
- `8>)A`
- `HB)A`
- `<F)A`
- `])A8`
- `(#)A`
- `s)AX`
- `l")A`
- `<A)A`
- `L[(A`
- `dF)A`
- `r)AP`
- `$A)A`
- `4r)A`
- `&)Al`
- `([)A<`
- `B)A$`
- `\f'A`
- `H>)A`
- `B)A<`
- `A)Ap`
- `$C)Ax`
- `\a)A`
- `,r)A`
- `Xe)A(`
- `,p)A`
- `lF)A`
- `(J)A`
- ` &)A,`
- `'>)A`
- `")Ah`
- `XE(A`
- `da)A\`
- `\n)AD`
- `p:)A`
- `,A)A`
- `X>)A`
- `G)A@`
- `$F)A`
- `!)Ap`
- `+)A8`
- `h#)A@`
- `,G)A@`
- `DA)A`
- `dZ)A`
- `s)AX`
- `4s)AP`
- `TF)A`
- `tF)A`
- `@>)A`
- ` _(A`
- `x&)AP`
- `$r)A`
- `lG)AD`
- `4A)A`
- `pE)A`
- `x:)A`
- `8#)A(`
- `\F)A`
- `d[)A`
- `b)A4`
- `;)A'`
- `P>)A`
- `-)AD`
- `<R)A`
- `@B)A`
- `PB)A<`
- `%)A(`
- `F)A@`
- `#)A$`
- `+)A,`
- `h>)A`
- `'#)A`
