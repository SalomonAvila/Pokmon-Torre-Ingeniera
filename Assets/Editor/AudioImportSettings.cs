using UnityEditor;
using UnityEngine;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Ajustes de importación del audio (Etapa 9): la música de <c>Resources/Audio/Musica/</c> se
    /// comprime en Vorbis y se descomprime al reproducir (ocupa poco en la build); los efectos de
    /// <c>Resources/Audio/Efectos/</c> se descomprimen al cargar para sonar sin retraso.
    /// La música conserva el estéreo y los efectos van en mono. Se aplica solo al importar; para
    /// reaplicarlo, reimporta la carpeta.
    /// </summary>
    public class AudioImportSettings : AssetPostprocessor
    {
        const string MusicFolder = "Assets/Resources/Audio/Musica/";
        const string EffectsFolder = "Assets/Resources/Audio/Efectos/";

        void OnPreprocessAudio()
        {
            bool music = assetPath.StartsWith(MusicFolder);
            bool effect = assetPath.StartsWith(EffectsFolder);
            if (!music && !effect)
                return;

            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = !music; // la música conserva el estéreo; los efectos van en mono
            importer.loadInBackground = music;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
        }
    }
}
