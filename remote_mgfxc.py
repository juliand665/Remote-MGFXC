#!/usr/bin/env python3

# NOTE: this script expects you to have a host specified in the variable $REMOTE_MGFXC_HOST
# if you're using fish, i'd recommend using `set -Ux REMOTE_MGFXC_HOST <hostname>` to set it permanently
# you can also set $REMOTE_MGFXC_PORT--the port defaults to 44321 if unset

import sys
import os
import requests

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


for input_filename in filenames:
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
        print(f"{Style.bright_black}Skipping {input_filename}{Style.reset}")
        continue

    # remove previous output
    if os.path.exists(output_filename):
        os.remove(output_filename)

    print(f"{Style.bold}Compiling {input_filename}{Style.reset}")

    # run the compiler
    url = f"http://{host}:{port}/compiler?filename={basename}"
    response = requests.post(url, data=open(input_filename, "rb"))
    if response.status_code == 200:
        with open(output_filename, "wb") as f:
            f.write(response.content)
        print(f"{Style.green}Successfully compiled!{Style.reset}")
    if response.status_code == 500:
        error_json = response.json()
        title = error_json["title"]
        detail = error_json["detail"]
        print(f"{Style.bright_red}{title} for {input_filename}:{Style.reset}")
        print(f"{Style.red}{detail}{Style.reset}")
print()
