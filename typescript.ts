  
  // returns a nicely humanized readout for file size
  formatBytes(bytes, decimals) {
    if (bytes === 0) {
      return '0 Bytes';
    }
    const k = 1024,
      dm = decimals <= 0 ? 0 : decimals || 2,
      sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'],
      i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
  }



 
exports.getFilesByRootFolderName = function (req, res) {

  const filesystem = require('fs');
  let RootFolderName = req.params.RootFolderName;
  let files;
  const projectFilesharePath = fileshareRootPath + RootFolderName;
  // this walks the file tree by given directory and assign all files
  // to an array _getAllFilesFromFolder
    var _getAllFilesFromFolder = function (dir) {
    let results = [];
    filesystem.readdirSync(dir).forEach(function (file) {

      let name = file.split('.')[0];
      let type = file.split('.')[1];
      let subFolderPath = dir;
      let folderArray = subFolderPath.split('\\');
      let path = dir + '\\' + file;
      file = dir + '\\' + file;
      let stat = filesystem.statSync(file);
    // let folder = folderArray[folderArray.length - 1];
      let folder = subFolderPath;

      if (stat && stat.isDirectory()) {
        results = results.concat(_getAllFilesFromFolder(file))
      } else {
        const f = {path: path, type: type, name: name, stats: stat, folder: folder};
        results.push(f);
      }
    });
    return results;
  }

  // create the fileshare folder if it doesn't already exist
  if (!filesystem.existsSync(projectFilesharePath)) {
    filesystem.mkdirSync(projectFilesharePath);
  }

  // call the variable from above to get files
  files = _getAllFilesFromFolder(projectFilesharePath);

  // send the files!
  if (files != null) {
    helpers.handleResults(null, files, res);
  } else {
    helpers.handleResults(null, {}, res);
  }
};
