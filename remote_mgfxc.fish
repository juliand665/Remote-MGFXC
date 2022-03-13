#!/usr/local/bin/fish

# NOTE: this function expects you to have a host specified in the variable $REMOTE_MGFXC_HOST
# i'd recommend using the fish command `set -Ux REMOTE_MGFXC_HOST <hostname>` to set it permanently
# you can also set $REMOTE_MGFXC_PORT--the port defaults to 44321 if unset

function remote_mgfxc
    set -l filenames $argv[1..]
    set -l should_skip 0
    if not set -q filenames[1]
        set filenames *.fx
        set should_skip 1
    end

    for filename in $filenames
        echo
        set -l basename (basename $filename .fx)
        set -l output_filename $basename.ogl.mgfxo
        set -l error_filename $basename.error.json
        rm -f $error_filename

        if command test $should_skip -eq 0 -o ! -e $output_filename -o $output_filename -ot $filename
            set_color -o
            echo "Compiling $filename"
            set_color normal
            rm -f $output_filename
            set -l port $REMOTE_MGFXC_PORT
            if not set -q port[1]
                set port 44321
            end
            curl -s --header "Content-Type:application/octet-stream" --data-binary @$filename "http://$REMOTE_MGFXC_HOST:$port/compiler?filename=$basename" --output $output_filename
            set -l curl_status $status

            if test $curl_status -ne 0
                set_color -o red
                if test $curl_status -eq 7
                    echo "Curl could not connect to the compiler server!"
                else
                    echo "Curl request failed with exit code $curl_status."
                end
            else if test ! -s $output_filename
                set_color -o red
                echo "Curl request succeeded but produced no output!"
            else if test (head -c 1 $output_filename) = "{"
                set_color brred
                set -l title (cat $output_filename | jq -r .title)
                echo "$title for $filename"
                set_color red
                cat $output_filename | jq -r .detail

                mv $output_filename $error_filename
            else
                set_color green
                echo "Successfully compiled $filename"
            end
        else
            set_color brblack
            echo "Skipping $filename"
        end
        set_color normal
    end
    echo
end
