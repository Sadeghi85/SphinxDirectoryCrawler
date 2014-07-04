/*
SQLyog Ultimate v11.22 (64 bit)
MySQL - 5.6.17-log : Database - sphinx_lib_eshia_ir
*********************************************************************
*/

/*!40101 SET NAMES utf8 */;

/*!40101 SET SQL_MODE=''*/;

/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE DATABASE /*!32312 IF NOT EXISTS*/`sphinx_lib_eshia_ir` /*!40100 DEFAULT CHARACTER SET utf8 */;

USE `sphinx_lib_eshia_ir`;

/*Table structure for table `documents` */

DROP TABLE IF EXISTS `documents`;

CREATE TABLE `documents` (
  `auto_id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `id` bigint(20) unsigned NOT NULL,
  `path` varchar(255) NOT NULL,
  `modified_at` int(10) unsigned NOT NULL,
  `bookid` int(10) unsigned NOT NULL,
  `volume` int(10) unsigned NOT NULL,
  `page` int(10) unsigned NOT NULL,
  PRIMARY KEY (`auto_id`),
  KEY `index_id` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

/*Table structure for table `documents_deleted` */

DROP TABLE IF EXISTS `documents_deleted`;

CREATE TABLE `documents_deleted` (
  `auto_id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `id` bigint(20) unsigned NOT NULL,
  PRIMARY KEY (`auto_id`),
  KEY `index_id` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

/*Table structure for table `documents_last_main_reindex` */

DROP TABLE IF EXISTS `documents_last_main_reindex`;

CREATE TABLE `documents_last_main_reindex` (
  `id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `last_reindex` int(10) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;
