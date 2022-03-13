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
            if not set -q port
                set -l port 44321
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
                set -l error
                begin
                    set -l IFS
                    set error (cat $output_filename | jq -r .detail | string replace -a '\r' '\n' | string replace -a '\\\\' '\\')
                end
                set -l first_line_parts (echo $error | head -1 | string split -m1 .fx)

                set -l location
                set -l first_line_rest
                if set -q first_line_parts[2]
                    set -l next_split (echo $first_line_parts[2] | string split -m1 ': ')
                    set location $next_split[1]
                    set first_line_rest $next_split[2]
                else
                    set first_line_rest $first_line_parts[1]
                end

                set_color brred
                set -l title (cat $output_filename | jq -r .title)
                echo -n "$title for $filename"
                set_color -o
                if set -q location[1]
                    echo " at $location"
                else
                    echo
                    echo $first_line_rest
                end
                set_color normal
                set_color red
                echo $first_line_rest
                echo -n $error | tail -n +2

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
