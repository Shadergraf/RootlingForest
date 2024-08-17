using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manatea.SceneManagement
{
    public static class MSceneManager
    {
        private const string k_PersistentSceneName = "[persistent_scene]";
        private static Scene m_PersistentScene;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            SceneManager.sceneUnloaded += SceneUnloaded;

            if (SceneManager.sceneCount > 0)
                EnsurePersistentScene();
        }


        //public static void LoadScene(SceneReference scene)
        //{
        //    SceneManager.LoadSceneAsync(SceneHelper.GetScenePath(scene), LoadSceneMode.Additive);
        //}


        private static void EnsurePersistentScene()
        {
            if (m_PersistentScene.IsValid())
                return;

            m_PersistentScene = SceneManager.CreateScene(k_PersistentSceneName, new CreateSceneParameters());
        }


        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsurePersistentScene();
        }
        private static void SceneUnloaded(Scene scene)
        {
            EnsurePersistentScene();
        }





        //Actual Scene loading
        public static async Task<AsyncOperation> LoadSceneAsync(SceneReference sceneRef, SceneLoadSettings settings = default(SceneLoadSettings))
        {
            var loadSceneMode = settings.LoadAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            var sceneName = SceneHelper.GetScenePath(sceneRef);
            var localPhysicsMode = (UnityEngine.SceneManagement.LocalPhysicsMode)(int)settings.LocalPhysicsMode;
            LoadSceneParameters loadSceneParams = new LoadSceneParameters(loadSceneMode, localPhysicsMode);

            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, loadSceneParams);
            asyncOp.allowSceneActivation = !settings.ActivateManually;

            await AwaitAsyncOperation(asyncOp);
            return asyncOp;
        }
        public static async Task<AsyncOperation> UnloadSceneAsync(SceneReference sceneRef, UnloadSceneOptions options = default(UnloadSceneOptions))
        {
            var sceneName = SceneHelper.GetScenePath(sceneRef);

            AsyncOperation asyncOp = SceneManager.UnloadSceneAsync(sceneName, options);

            await AwaitAsyncOperation(asyncOp);
            return asyncOp;
        }

        private static async Task AwaitAsyncOperation(AsyncOperation asyncOp)
        {
            while (asyncOp.progress < 0.9f)
                await Task.Delay(16);
        }
    }


    public struct SceneLoadSettings
    {
        private bool m_LoadAdditive;
        private LocalPhysicsMode m_LocalPhysicsMode;
        private bool m_ActivateManually;

        /// <summary>
        /// Should the scene be loaded additively?
        /// </summary>
        public bool LoadAdditive
        {
            get
            {
                return m_LoadAdditive;
            }
            set
            {
                m_LoadAdditive = value;
            }
        }
        /// <summary>
        /// The local physics mode of the scene
        /// </summary>
        public LocalPhysicsMode LocalPhysicsMode
        {
            get
            {
                return m_LocalPhysicsMode;
            }
            set
            {
                m_LocalPhysicsMode = value;
            }
        }
        /// <summary>
        /// Should the scene be activated manually?
        /// </summary>
        public bool ActivateManually
        {
            get
            {
                return m_ActivateManually;
            }
            set
            {
                m_ActivateManually = value;
            }
        }


        /// <summary>
        /// Constructor for SceneLoadSettings.
        /// </summary>
        /// <param name="additive">Should the scene be loaded additively?</param>
        public SceneLoadSettings(bool additive)
        {
            m_LoadAdditive = additive;
            m_LocalPhysicsMode = LocalPhysicsMode.None;
            m_ActivateManually = false;
        }
        /// <summary>
        /// Constructor for SceneLoadSettings.
        /// </summary>
        /// <param name="additive">Should the scene be loaded additively?</param>
        /// <param name="physicsMode">The local physics mode of the scene</param>
        public SceneLoadSettings(bool additive, LocalPhysicsMode physicsMode)
        {
            m_LoadAdditive = additive;
            m_LocalPhysicsMode = physicsMode;
            m_ActivateManually = false;
        }
        /// <summary>
        /// Constructor for SceneLoadSettings.
        /// </summary>
        /// <param name="additive">Should the scene be loaded additively?</param>
        /// <param name="physicsMode">The local physics mode of the scene</param>
        /// <param name="activateManually">Should the scene be activated manually?</param>
        public SceneLoadSettings(bool additive, LocalPhysicsMode physicsMode, bool activateManually)
        {
            m_LoadAdditive = additive;
            m_LocalPhysicsMode = physicsMode;
            m_ActivateManually = activateManually;
        }
    }

    /// <summary>
    /// Provides options for 2D and 3D local physics.
    /// </summary>
    [Flags]
    public enum LocalPhysicsMode
    {
        /// <summary>
        /// No local 2D or 3D physics Scene will be created.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// A local 2D physics Scene will be created and owned by the Scene.
        /// </summary>
        Physics2D = 0x1,
        /// <summary>
        /// A local 3D physics Scene will be created and owned by the Scene.
        /// </summary>
        Physics3D = 0x2
    }
}
