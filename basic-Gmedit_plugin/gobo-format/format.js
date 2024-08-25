const { execFile } = require('child_process');
const { dir } = require('console');
const path = require('path');


(function(){
    function currFile() { return $gmedit["gml.file.GmlFile"].current; }
    GMEdit.register("gobo-format", {

         init: function(_state) {



            aceEditor.commands.addCommand({
                name: "replace",
                bindKey: {win: "Ctrl-K", mac: "Command-K"},
                exec: function(editor) {
                    // Log the path of the GML file
                    //console.log(aceEditor.session.gmlFile.path);

                    // Get the path of the GML file
                    //filePath = aceEditor.session.gmlFile.path; currFile().path
                    filePath = $gmedit["gml.file.GmlFile"].current.path
                    //console.log(`File path: ${filePath}`);
                    
                    // Convert the file path to a directory path
                    directoryPath = path.dirname(filePath);
                    //console.log(`Directory path: ${directoryPath}`);
                    
                    // Define the path to your executable
                    const exePath = path.join(_state.dir,'gobo.exe');

                    // Call the executable
                    execFile(exePath, [directoryPath], (error, stdout, stderr) => {
                        if (error) {
                            console.error(`Error executing file: ${error}`);
                            return;
                        }
                        // Log the output from the executable
                        console.log(`stdout: ${stdout}`);
                        console.log(`stdout: ${$gmedit["gml.file.GmlFile"].current.name}`);
                        console.log(`stdout: ${$gmedit["gml.file.GmlFile"].current}`);
                        // console.error(`stderr: ${stderr}`);
                        // console.error(`stderr: ${$gmedit["gml.file.GmlFile"].current}`);
                        // console.error(`stderr: ${aceEditor.session.gmlFile}`);
                        // console.error(`stderr: ${currFile().editor}`);
                        // console.error(`stderr: ${currFile().codeEditor}`);
                        //alert(stdout)
                        alert(stderr);

                        //$gmedit["gml.file.GmlFile"].current.set_changed(true)

                        var temp = ""
                        $gmedit["gml.file.GmlFile"].current.codeEditor.checkChanges();
                        $gmedit["gml.file.GmlFile"].current.codeEditor.load();
                        aceEditor.session.bgTokenizer.start(0);
                        // reopen file?

                       // GMEdit._signal("activeFileChange", currFile())
                        //GMEdit._signal("activeFileChange", currFile())
                        //editors
                        //GMEdit.fileReload(aceEditor.session.gmlFile)
                        //GMEdit.fileReload(filePath)
                        //aceEditor.checkChanges()
                        //aceEditor.session.gmlFile.checkChanges();
                       // editor(filePath)
                    });






                }
            });

        }
    
        //init: init,

    });


})();
