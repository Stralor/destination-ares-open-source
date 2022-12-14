using System.IO;
using UnityEditor;

namespace SuperSystems.UnityBuild
{

[System.Serializable]
public class BuildPlatform
{
    public bool enabled = false;
    public BuildDistributionList distributionList = new BuildDistributionList();
    public BuildArchitecture[] architectures = new BuildArchitecture[0];
    public BuildVariant[] variants = new BuildVariant[0];

    public string platformName;
    public string binaryNameFormat;
    public string dataDirNameFormat;
    public BuildTargetGroup targetGroup;
    
    public virtual void Init()
    {
    }

    #region Public Properties

    public bool atLeastOneArch
    {
        get
        {
            bool atLeastOneArch = false;
            for (int i = 0; i < architectures.Length && !atLeastOneArch; i++)
            {
                atLeastOneArch |= architectures[i].enabled;
            }

            return atLeastOneArch;
        }
    }

    public bool atLeastOneDistribution
    {
        get
        {
            bool atLeastOneDist = false;
            for (int i = 0; i < distributionList.distributions.Length && !atLeastOneDist; i++)
            {
                atLeastOneDist |= distributionList.distributions[i].enabled;
            }

            return atLeastOneDist;
        }
    }

    #endregion
}

}