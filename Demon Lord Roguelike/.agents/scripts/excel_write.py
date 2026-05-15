"""
Excel 写入工具 - 使用 openpyxl，UTF-8
用法: python excel_write.py --path <文件路径> --sheet <Sheet名> --row <行> --col <列> --value <值> [--backup]
也支持 --find-col <列名> --find-id <ID值> 按列名定位行
"""
import argparse
import shutil
import sys
import openpyxl


def main():
    parser = argparse.ArgumentParser(description="写入/修改 Excel 单元格")
    parser.add_argument("--path", required=True, help="Excel 文件路径")
    parser.add_argument("--sheet", default=None, help="Sheet 名称（默认第一个）")
    parser.add_argument("--row", type=int, default=None, help="1-based 行号")
    parser.add_argument("--col", type=int, default=None, help="1-based 列号")
    parser.add_argument("--value", default=None, help="要写入的值")
    parser.add_argument("--backup", action="store_true", help="写入前备份原文件（追加 .bak）")
    # 按列名+ID定位
    parser.add_argument("--find-col", default=None, help="用于匹配的列名（如 'id'）")
    parser.add_argument("--find-id", default=None, help="要匹配的值")
    parser.add_argument("--set-col", default=None, help="要修改的列名")
    args = parser.parse_args()

    # 备份
    if args.backup:
        shutil.copy2(args.path, args.path + ".bak")
        print(f"已备份: {args.path}.bak")

    try:
        wb = openpyxl.load_workbook(args.path)
    except FileNotFoundError:
        print(f"错误: 文件不存在 -> {args.path}", file=sys.stderr)
        sys.exit(1)

    ws = wb[args.sheet] if args.sheet else wb.active

    # 模式一：直接指定行列
    if args.row is not None and args.col is not None and args.value is not None:
        old_value = ws.cell(row=args.row, column=args.col).value
        ws.cell(row=args.row, column=args.col).value = _parse_value(args.value)
        print(f"已修改 ({args.row}, {args.col}): {old_value!r} -> {args.value!r}")

    # 模式二：按列名查找行
    elif args.find_col and args.find_id and args.set_col and args.value is not None:
        headers = {cell.value: cell.column for cell in ws[1]}
        if args.find_col not in headers:
            print(f"错误: 查找列 '{args.find_col}' 不存在", file=sys.stderr)
            wb.close()
            sys.exit(1)
        if args.set_col not in headers:
            print(f"错误: 目标列 '{args.set_col}' 不存在", file=sys.stderr)
            wb.close()
            sys.exit(1)

        find_col_idx = headers[args.find_col]
        set_col_idx = headers[args.set_col]
        found = False
        for row_idx in range(2, ws.max_row + 1):
            cell_val = ws.cell(row=row_idx, column=find_col_idx).value
            if str(cell_val) == str(args.find_id):
                old_value = ws.cell(row=row_idx, column=set_col_idx).value
                ws.cell(row=row_idx, column=set_col_idx).value = _parse_value(args.value)
                print(f"已修改 行{row_idx} [{args.set_col}]: {old_value!r} -> {args.value!r}")
                found = True
                break
        if not found:
            print(f"未找到 {args.find_col}={args.find_id}", file=sys.stderr)
            wb.close()
            sys.exit(1)
    else:
        print("错误: 请提供 (--row --col --value) 或 (--find-col --find-id --set-col --value)", file=sys.stderr)
        wb.close()
        sys.exit(1)

    wb.save(args.path)
    wb.close()
    print(f"已保存: {args.path}")


def _parse_value(raw: str):
    """尝试将字符串转换为 int 或 float，否则保留字符串。"""
    try:
        return int(raw)
    except ValueError:
        pass
    try:
        return float(raw)
    except ValueError:
        pass
    return raw


if __name__ == "__main__":
    main()
