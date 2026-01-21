import type {ReactNode} from 'react';
import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

type FeatureItem = {
  title: string;
  icon: string;
  description: ReactNode;
};

const FeatureList: FeatureItem[] = [
  {
    title: 'Multiple Validators',
    icon: '✅',
    description: (
      <>
        Built-in validators for common scenarios: required, min/max length, 
        equality, ranges, and more. Combine multiple validators for complex validation logic.
      </>
    ),
  },
  {
    title: 'Security First',
    icon: '🔒',
    description: (
      <>
        Built-in DoS protection, path traversal prevention, and resource limits. 
        Designed with security best practices to protect your applications.
      </>
    ),
  },
  {
    title: 'Multi-Framework Support',
    icon: '🎯',
    description: (
      <>
        Supports .NET 8.0, 9.0, and 10.0. Works with JSON and YAML configuration files.
        Full dependency injection support for modern .NET applications.
      </>
    ),
  },
  {
    title: 'Performance Optimized',
    icon: '⚡',
    description: (
      <>
        Fast validation with caching, streaming support, and parallel processing.
        Minimal overhead for production environments.
      </>
    ),
  },
  {
    title: 'CI/CD Ready',
    icon: '🚀',
    description: (
      <>
        Validate configurations in your deployment pipelines. Catch issues before
        they reach production with command-line interface support.
      </>
    ),
  },
  {
    title: 'Extensible',
    icon: '🔧',
    description: (
      <>
        Create custom validators to match your specific needs. Plugin architecture
        allows easy extension of validation capabilities.
      </>
    ),
  },
];

function Feature({title, icon, description}: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <div className={styles.featureIcon}>{icon}</div>
      </div>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures(): ReactNode {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
