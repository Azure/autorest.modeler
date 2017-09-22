task 'publish-preview', '', ['version-number','build'] , (done) ->
  package_json = require "#{basefolder}/package.json"

  # Note : this will call the npm prepare task, which will call 
  rm "-f", "#{basefolder}/.gitignore"
  execute "#{basefolder}/node_modules/.bin/yarn publish --tag preview --new-version #{version} --access public ",{cwd:basefolder, silent:false }, (c,o,e) -> 
  # execute "npm publish --tag preview --access public ",{cwd:basefolder, silent:false }, (c,o,e) -> 
    echo  "\n\nPublished:  #{package_json.name}@#{info version} (tagged as @preview)\n\n"
    execute "git checkout #{basefolder}/.gitignore",{cwd:basefolder, silent:true }, done
