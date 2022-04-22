#!/usr/bin/env python3

# NOTE: this script expects you to have a host specified in the variable $REMOTE_MGFXC_HOST
# if you're using fish, i'd recommend using `set -Ux REMOTE_MGFXC_HOST <hostname>` to set it permanently
# you can also set $REMOTE_MGFXC_PORT--the port defaults to 44321 if unset

import os
import sys
import re
import requests
from typing import Dict

host = os.getenv("REMOTE_MGFXC_HOST")
port = os.getenv("REMOTE_MGFXC_PORT") or 44321
if host is None:
    print("REMOTE_MGFXC_HOST not set!")
    sys.exit(1)

filenames = sys.argv[1:]
force_recompile = True

if len(filenames) == 0:
    filenames = [f for f in os.listdir(".") if f.endswith(".fx")]
    force_recompile = False


class Style:
    reset = "\033[0m"
    bold = "\033[1m"
    red = "\033[31m"
    green = "\033[32m"
    bright_black = "\033[90m"
    bright_red = "\033[91m"


def print_styled(text: str, style: str):
    print(style, end="")
    print(text, end="")
    print(Style.reset)


# read input files
files: Dict[str, str] = {}


def read_file(filename: str):
    with open(filename, "r", encoding="utf-8-sig") as file:
        files[filename] = file.read()


for filename in filenames:
    read_file(filename)

root_filenames = set(filenames)


def inline_includes(filename: str):
    code = files[filename]
    rewritten_code = ""
    include_regex = r'\s*#include\s+"([^"]+)"\s*'
    for line in code.split("\n"):
        match = re.match(include_regex, line)
        if match:
            include_filename = match.group(1)
            if include_filename in root_filenames:
                root_filenames.remove(include_filename)
            if include_filename not in files:
                read_file(include_filename)

            inline_includes(include_filename)

            rewritten_code += f"// begin included file: {include_filename}\n"
            rewritten_code += files[include_filename] + "\n"
            rewritten_code += f"// end included file: {include_filename}\n"
            rewritten_code += "\n"
        else:
            rewritten_code += line + "\n"
    files[filename] = rewritten_code


# inline included files
for filename in filenames:
    inline_includes(filename)


for input_filename in root_filenames:
    print()
    # basename with stripped .fx extension
    basename, extension = os.path.splitext(os.path.basename(input_filename))
    assert extension == ".fx"
    output_filename = f"{basename}.ogl.mgfxo"

    if (
        not force_recompile
        and os.path.exists(output_filename)
        and (os.path.getmtime(output_filename) > os.path.getmtime(input_filename))
    ):
        print_styled(f"Skipping {input_filename}", Style.bright_black)
        continue

    # remove previous output
    if os.path.exists(output_filename):
        os.remove(output_filename)

    print_styled(f"Compiling {input_filename}", Style.bold)

    # run the compiler
    url = f"http://{host}:{port}/compiler?filename={basename}"
    # print(files[input_filename])
    response = requests.post(url, data=files[input_filename].encode("utf-8"))

    if response.status_code == 200:
        with open(output_filename, "wb") as output_file:
            output_file.write(response.content)
        print_styled("Successfully compiled!", Style.green)
    elif response.status_code == 500:
        error_json = response.json()
        title = error_json["title"]
        detail = error_json["detail"]
        print_styled(f"{title} for {input_filename}:", Style.bright_red)
        print_styled(detail, Style.red)
    else:
        print_styled(f"Unknown error code: {response.status_code}", Style.bright_red)
print()
