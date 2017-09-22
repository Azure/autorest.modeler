task 'publish-preview', '', ['version-number'] , (done) ->
  package_json = require "#{basefolder}/package.json"

  # Note : this will call the npm prepare task, which will call 
  execute "#{basefolder}/node_modules/.bin/yarn publish --tag preview --new-version #{version} --access public ",{cwd:basefolder, silent:false }, (c,o,e) -> 
    echo  "\n\nPublished:  #{package_json.name}@#{info version} (tagged as @preview)\n\n"
    done()
    